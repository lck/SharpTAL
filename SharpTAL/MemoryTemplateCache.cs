using System;
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
	public class MemoryTemplateCache : AbstractTemplateCache
	{
		object m_CacheLock;
		Dictionary<string, TemplateInfo> m_Cache;

		/// <summary>
		/// Initialize the template cache
		/// </summary>
		public MemoryTemplateCache()
		{
			m_CacheLock = new object();
			m_Cache = new Dictionary<string, TemplateInfo>();
		}

		protected override TemplateInfo GetTemplateInfo(string templateBody, Dictionary<string, object> globals,
			Dictionary<string, string> inlineTemplates, List<Assembly> referencedAssemblies)
		{
			lock (m_CacheLock)
			{
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

				// Generate source
				SourceGenerator sourceGenerator = new SourceGenerator();
				ti.GeneratedSourceCode = sourceGenerator.GenerateSource(ti);

				// Generate assembly
				AssemblyGenerator assemblyCompiler = new AssemblyGenerator();
				Assembly assembly = assemblyCompiler.GenerateAssembly(ti, true, null, null);

				// Try to load the Render() method from assembly
				ti.TemplateRenderMethod = GetTemplateRenderMethod(assembly, ti);

				m_Cache.Add(ti.TemplateHash, ti);

				return ti;
			}
		}
	}
}
