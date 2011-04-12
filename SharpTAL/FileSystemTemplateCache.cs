﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.CodeDom.Compiler;
using System.Security.Cryptography;

using Microsoft.CSharp;

using SharpTAL.Interfaces;

namespace SharpTAL
{
	public class FileSystemTemplateCache : AbstractTemplateCache
	{
		const string DEFAULT_FILENAME_PATTER = @"Template_{key}.dll";
		const string SHA1_KEY_PATTERN = @"[a-zA-Z0-9]{38}";

		object m_CacheLock;
		string m_CacheFolder;
		Dictionary<string, TemplateInfo> m_Cache;
		string m_Pattern;
		Regex m_PatternRex;

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
		/// <param name="clearCache">If it's true, clear all files matching the <paramref name="pattern"/> from cache folder</param>
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
			// Check cache folder
			if (!Directory.Exists(cacheFolder))
			{
				throw new ArgumentException(string.Format("Template cache folder does not exists: [{0}]", cacheFolder));
			}

			m_CacheLock = new object();
			m_CacheFolder = cacheFolder;
			m_Cache = null;

			//
			// Setup pattern
			//

			// If input pattern is "Template_{key}.dll"
			// then result pattern is "(^Template_)(?<key>[a-zA-Z0-9]{38})(\.dll$)"
			m_Pattern = pattern;
			if (string.IsNullOrEmpty(pattern))
			{
				m_Pattern = DEFAULT_FILENAME_PATTER;
			}

			string[] patternGroups = m_Pattern.Split(new string[] { "{key}" }, StringSplitOptions.None);

			// Check if pattern contains exactly one "{key}" macro
			if (patternGroups.Length != 2)
			{
				throw new ArgumentException(
					string.Format(@"Invalid pattern specified. Macro ""{key}"" is missing or specified more than once: [{0}]",
					m_Pattern));
			}

			// Normalize pattern
			string rexPattern = string.Format(@"(^{0})(?<key>{1})({2}$)",
				patternGroups[0].Replace(".", @"\."),
				SHA1_KEY_PATTERN,
				patternGroups[1].Replace(".", @"\."));
			m_PatternRex = new Regex(rexPattern);

			// Delete all files from cache folder matching the pattern
			if (clearCache)
			{
				DirectoryInfo di = new DirectoryInfo(cacheFolder);
				foreach (FileInfo fi in di.GetFiles())
				{
					if (m_PatternRex.Match(fi.Name).Success)
					{
						File.Delete(fi.FullName);
					}
				}
			}
		}

		protected override TemplateInfo GetTemplateInfo(string templateBody, Dictionary<string, object> globals,
			Dictionary<string, string> inlineTemplates, List<Assembly> referencedAssemblies)
		{
			lock (m_CacheLock)
			{
				// Cache is empty, load templates from cache folder
				if (m_Cache == null)
				{
					m_Cache = LoadTemplatesInfo(m_CacheFolder);
				}

				// Create template info
				Dictionary<string, Type> globalsTypes = new Dictionary<string, Type>();
				if (globals != null)
				{
					foreach (string objName in globals.Keys)
					{
						object obj = globals[objName];
						globalsTypes.Add(objName, obj != null ? obj.GetType() : null);
					}
				}
				TemplateInfo ti = new TemplateInfo()
				{
					TemplateBody = templateBody,
					TemplateHash = null,
					GlobalsTypes = globalsTypes,
					ReferencedAssemblies = referencedAssemblies,
					InlineTemplates = inlineTemplates,
					TemplateRenderMethod = null
				};

				// Compile Template to TALPrograms and generate the TemplateHash
				TALCompiler.CompileTemplate(ti);

				// Generated template found in cache
				if (m_Cache.ContainsKey(ti.TemplateHash))
				{
					return m_Cache[ti.TemplateHash];
				}

				// Path to output assembly
				string assemblyFileName = m_Pattern.Replace("{key}", ti.TemplateHash);
				string assemblyPath = Path.Combine(m_CacheFolder, assemblyFileName);

				// Generate source
				SourceGenerator sourceGenerator = new SourceGenerator();
				ti.GeneratedSourceCode = sourceGenerator.GenerateSource(ti);

				// Generate assembly
				AssemblyGenerator assemblyCompiler = new AssemblyGenerator();
				Assembly assembly = assemblyCompiler.GenerateAssembly(ti, false, assemblyPath, null);

				// Try to load the Render() method from assembly
				ti.TemplateRenderMethod = GetTemplateRenderMethod(assembly, ti);

				m_Cache.Add(ti.TemplateHash, ti);

				return ti;
			}
		}

		private Dictionary<string, TemplateInfo> LoadTemplatesInfo(string cacheFolder)
		{
			Dictionary<string, TemplateInfo> templateCache = new Dictionary<string, TemplateInfo>();

			// Load assemblies containing type with full name [<TEMPLATE_NAMESPACE>.<TEMPLATE_TYPENAME>],
			// with one public method [public static string Render(Dictionary<string, object> globals)]

			DirectoryInfo di = new DirectoryInfo(cacheFolder);
			foreach (FileInfo fi in di.GetFiles())
			{
				Match fileNameMatch = m_PatternRex.Match(fi.Name);
				if (fileNameMatch.Success)
				{
					// Try to load file as assembly
					try
					{
						AssemblyName.GetAssemblyName(fi.FullName);
					}
					catch (System.BadImageFormatException)
					{
						// The file is not an Assembly
						continue;
					}

					// Read assembly
					Assembly assembly = Utils.ReadAssembly(fi.FullName);

					// Get template hash from file name
					string templateHash = fileNameMatch.Groups["key"].Value;

					// Create template info
					TemplateInfo ti = new TemplateInfo() { TemplateHash = templateHash };

					// Try to load the Render() method from assembly
					ti.TemplateRenderMethod = GetTemplateRenderMethod(assembly, ti);

					templateCache.Add(templateHash, ti);
				}
			}

			return templateCache;
		}
	}
}
