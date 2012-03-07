//
// AbstractTemplateCache.cs
//
// Author:
//   Roman Lacko (backup.rlacko@gmail.com)
//
// Copyright (c) 2010 - 2012 Roman Lacko
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

namespace SharpTAL.TemplateCache
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Reflection;
	using System.Globalization;
	using SharpTAL.TemplateProgram;

	public abstract class AbstractTemplateCache : ITemplateCache
	{
		/// <summary>
		/// Compile template to ensure that the compiled assembly is already in cache when
		/// RenderTemplate is called for the first time. For precompiling, the actual values
		/// of globals are not required, just the names and types of the global variables.
		/// </summary>
		/// <param name="templateBody">The template body</param>
		/// <param name="globals">Dictionary of global variables</param>
		/// <param name="referencedAssemblies">List of referenced assemblies</param>
		/// <returns>The TemplateInfo generated from compiled template body</returns>
		public abstract TemplateInfo CompileTemplate(string templateBody, Dictionary<string, Type> globalsTypes, List<Assembly> referencedAssemblies);

		/// <summary>
		/// Returns a unique key that represent the template in the template cache.
		/// The template key is computed from the following parts:
		///	 Template body hash,
		///	 Global types hash,
		///	 Imported templates hash,
		///	 Referenced assemblies hash
		/// </summary>
		/// <param name="ti">Template info</param>
		/// <returns>Template key</returns>
		protected static string ComputeTemplateKey(TemplateInfo ti)
		{
			// Template body hash
			string hash = Utils.ComputeHash(ti.TemplateBody);

			// Global types hash
			hash += Utils.ComputeHash(ti.GlobalsTypes);

			// Imported templates hash
			if (ti.ImportedPrograms != null && ti.ImportedPrograms.Count > 0)
			{
				List<string> keys = new List<string>(ti.ImportedPrograms.Keys);
				keys.Sort();
				foreach (string path in keys)
				{
					hash += Utils.ComputeHash(ti.ImportedPrograms[path].TemplateBody);
				}
			}

			// Referenced assemblies hash
			hash += Utils.ComputeHash(ti.ReferencedAssemblies);

			// Template Key
			string templateKey = Utils.ComputeHash(hash);
			return templateKey;
		}

		/// <summary>
		/// Generate template program from template body and generate the TemplateKey
		/// </summary>
		/// <param name="templateBody"></param>
		/// <param name="globalsTypes"></param>
		/// <param name="referencedAssemblies"></param>
		/// <returns></returns>
		protected static TemplateInfo GenerateTemplateProgram(string templateBody, Dictionary<string, Type> globalsTypes, List<Assembly> referencedAssemblies)
		{
			ProgramGenerator pageTemplateParser = new ProgramGenerator();
			TemplateInfo ti = new TemplateInfo
			{
				TemplateBody = templateBody,
				GlobalsTypes = globalsTypes,
				ReferencedAssemblies = referencedAssemblies
			};
			pageTemplateParser.GenerateTemplateProgram(ref ti);
			ti.TemplateKey = ComputeTemplateKey(ti);
			return ti;
		}

		/// <summary>
		/// Try to find the render method in assembly
		/// </summary>
		/// <param name="assembly"></param>
		/// <param name="ti"></param>
		/// <returns></returns>
		protected static MethodInfo GetTemplateRenderMethod(Assembly assembly, TemplateInfo ti)
		{
			string templateTypeFullName = string.Format("Templates.Template_{0}", ti.TemplateKey);

			// Check if assembly contains the template type
			Type templateType = assembly.GetType(templateTypeFullName);
			if (templateType == null)
			{
				throw new Exception(string.Format("Failed to find type [{0}] in assembly [{1}].",
					templateTypeFullName, assembly.FullName));
			}

			// Check if the template type has method [public static void Render(StreamWriter, IRenderContext)]
			MethodInfo renderMethod = templateType.GetMethod("Render",
				BindingFlags.Public | BindingFlags.Static,
				null, new Type[] { typeof(StreamWriter), typeof(IRenderContext) }, null);

			if (renderMethod == null || renderMethod.ReturnType.FullName != "System.Void")
			{
				throw new Exception(string.Format(@"Failed to find Render method in type [{0}] in assembly [{1}].
The signature of method must be [static void Render(StreamWriter output, IRenderContext context)]",
					templateTypeFullName, assembly.FullName));
			}

			return renderMethod;
		}
	}
}
