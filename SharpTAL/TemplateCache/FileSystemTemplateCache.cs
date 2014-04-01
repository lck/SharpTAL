//
// FileSystemTemplateCache.cs
//
// Author:
//   Roman Lacko (backup.rlacko@gmail.com)
//
// Copyright (c) 2010 - 2014 Roman Lacko
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SharpTAL.TemplateCache
{
	public class FileSystemTemplateCache : AbstractTemplateCache
	{
		private const string DefaultFilenamePatter = @"Template_{key}.dll";
		private const string Sha1KeyPattern = @"[a-zA-Z0-9]{38}";

		private Dictionary<string, TemplateInfo> _templateInfoCache;
		private readonly object _templateInfoCacheLock;
		private readonly string _templateCacheFolder;
		private readonly string _fileNamePattern;
		private Regex _fileNamePatternRegex;

		/// <summary>
		/// Initialize the filesystem template cache
		/// </summary>
		/// <param name="cacheFolder">Folder where cache will write and read generated assemblies</param>
		public FileSystemTemplateCache(string cacheFolder)
			: this(cacheFolder, false, null)
		{
		}

		/// <summary>
		/// Initialize the filesystem template cache
		/// </summary>
		/// <param name="cacheFolder">Folder where cache will write and read generated assemblies</param>
		/// <param name="clearCache">If it's true, clear all files matching the default pattern from cache folder</param>
		public FileSystemTemplateCache(string cacheFolder, bool clearCache)
			: this(cacheFolder, clearCache, null)
		{
		}

		/// <summary>
		/// Initialize the filesystem template cache
		/// </summary>
		/// <param name="cacheFolder">Folder where cache will write and read generated assemblies</param>
		/// <param name="clearCache">If it's true, clear all files matching the <paramref name="pattern"/> from cache folder</param>
		/// <param name="pattern">
		/// File name regular expression pattern.
		/// Default pattern is "Template_{key}.dll".
		/// Macro {key} will be replaced with computed hash key."
		/// </param>
		public FileSystemTemplateCache(string cacheFolder, bool clearCache, string pattern)
		{
			_templateInfoCacheLock = new object();
			_templateCacheFolder = cacheFolder;
			_templateInfoCache = null;
			_fileNamePattern = string.IsNullOrEmpty(pattern) ? DefaultFilenamePatter : pattern;

			InitCache(clearCache);
		}

		public override TemplateInfo CompileTemplate(string templateBody, Dictionary<string, Type> globalsTypes, List<Assembly> referencedAssemblies)
		{
			lock (_templateInfoCacheLock)
			{
				// Cache is empty, load templates from cache folder
				if (_templateInfoCache == null)
				{
					_templateInfoCache = LoadTemplatesInfo(_templateCacheFolder);
				}

				// Generate template program
				TemplateInfo ti = GenerateTemplateProgram(templateBody, globalsTypes, referencedAssemblies);

				// Generated template found in cache
				if (_templateInfoCache.ContainsKey(ti.TemplateKey))
				{
					return _templateInfoCache[ti.TemplateKey];
				}

				// Generate code
				ICodeGenerator codeGenerator = new CodeGenerator();
				ti.GeneratedSourceCode = codeGenerator.GenerateCode(ti);

				// Path to output assembly
				string assemblyFileName = _fileNamePattern.Replace("{key}", ti.TemplateKey);
				string assemblyPath = Path.Combine(_templateCacheFolder, assemblyFileName);

				// Generate assembly
				var assemblyCompiler = new AssemblyGenerator();
				Assembly assembly = assemblyCompiler.GenerateAssembly(ti, false, assemblyPath, null);

				// Try to load the Render() method from assembly
				ti.RenderMethod = GetTemplateRenderMethod(assembly, ti);

				// Try to load the template generator version from assembly
				ti.GeneratorVersion = GetTemplateGeneratorVersion(assembly, ti);

				_templateInfoCache.Add(ti.TemplateKey, ti);

				return ti;
			}
		}

		void InitCache(bool clearCache)
		{
			// Check cache folder
			if (!Directory.Exists(_templateCacheFolder))
			{
				throw new ArgumentException(string.Format("Template cache folder does not exists: [{0}]", _templateCacheFolder));
			}

			// Setup pattern
			// If input pattern is "Template_{key}.dll"
			// then result pattern is "(^Template_)(?<key>[a-zA-Z0-9]{38})(\.dll$)"
			string[] patternGroups = _fileNamePattern.Split(new[] { "{key}" }, StringSplitOptions.None);

			// Check if pattern contains exactly one "{key}" macro
			if (patternGroups.Length != 2)
			{
				throw new ArgumentException(
					string.Format(@"Invalid pattern specified. Macro ""{{key}}"" is missing or specified more than once: [{0}]",
					_fileNamePattern));
			}

			// Normalize pattern
			string rePattern = string.Format(@"(^{0})(?<key>{1})({2}$)",
				patternGroups[0].Replace(".", @"\."),
				Sha1KeyPattern,
				patternGroups[1].Replace(".", @"\."));
			_fileNamePatternRegex = new Regex(rePattern);

			// Delete all files from cache folder matching the pattern
			if (clearCache)
			{
				var di = new DirectoryInfo(_templateCacheFolder);
				foreach (FileInfo fi in di.GetFiles())
				{
					if (_fileNamePatternRegex.Match(fi.Name).Success)
					{
						File.Delete(fi.FullName);
					}
				}
			}
		}

		Dictionary<string, TemplateInfo> LoadTemplatesInfo(string cacheFolder)
		{
			var templateCache = new Dictionary<string, TemplateInfo>();

			// Load assemblies containing type with full name [<TEMPLATE_NAMESPACE>.<TEMPLATE_TYPENAME>],
			// with one public method [public static string Render(Dictionary<string, object> globals)]

			var di = new DirectoryInfo(cacheFolder);
			foreach (FileInfo fi in di.GetFiles())
			{
				Match fileNameMatch = _fileNamePatternRegex.Match(fi.Name);
				if (fileNameMatch.Success)
				{
					// Try to load file as assembly
					try
					{
						AssemblyName.GetAssemblyName(fi.FullName);
					}
					catch (BadImageFormatException)
					{
						// The file is not an Assembly
						continue;
					}

					// Get template hash from file name
					string templateHash = fileNameMatch.Groups["key"].Value;

					// Create template info
					var ti = new TemplateInfo { TemplateKey = templateHash };

					// Try to load the Render() method from assembly
					Assembly assembly = Utils.ReadAssembly(fi.FullName);
					ti.RenderMethod = GetTemplateRenderMethod(assembly, ti);

					// Try to load the template generator version from assembly
					ti.GeneratorVersion = GetTemplateGeneratorVersion(assembly, ti);

					templateCache.Add(templateHash, ti);
				}
			}

			return templateCache;
		}
	}
}
