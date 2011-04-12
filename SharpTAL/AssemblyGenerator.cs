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
	class AssemblyGenerator
	{
		public Assembly GenerateAssembly(TemplateInfo ti, bool generateInMemory, string assemblyPath, string keyFileName)
		{
			//---------------------------
			// Create compiler parameters
			//---------------------------

			CompilerParameters compilerParameters = new CompilerParameters();
			compilerParameters.GenerateExecutable = false;
			compilerParameters.GenerateInMemory = generateInMemory;
			compilerParameters.IncludeDebugInformation = false;
			compilerParameters.WarningLevel = 4;
			compilerParameters.TreatWarningsAsErrors = false;
			compilerParameters.TempFiles = new TempFileCollection(Path.GetTempPath(), false);
			compilerParameters.OutputAssembly = assemblyPath;

			if (!string.IsNullOrEmpty(keyFileName))
			{
				compilerParameters.CompilerOptions = string.Format("/keyfile:{0}", keyFileName);
			}

			//---------------------------
			// Setup referenced assemblies for code compiler
			//---------------------------

			// Add core assemblies
			List<string> assemblies = new List<string>()
            {
                "System.dll",
                "System.Core.dll",
                "System.Security.dll"
            };
			foreach (string asmName in assemblies)
			{
				compilerParameters.ReferencedAssemblies.Add(asmName);
			}

			// Add assemblies where global types are declared
			if (ti.GlobalsTypes != null)
			{
				foreach (string varName in ti.GlobalsTypes.Keys)
				{
					Type type = ti.GlobalsTypes[varName];
					if (type != null)
					{
						if (!assemblies.Contains(type.Assembly.Location) &&
							!assemblies.Contains(Path.GetFileName(type.Assembly.Location)))
						{
							compilerParameters.ReferencedAssemblies.Add(type.Assembly.Location);
							assemblies.Add(type.Assembly.Location);

							// Referenced assemblies
							foreach (AssemblyName assemblyName in type.Assembly.GetReferencedAssemblies())
							{
								Assembly assembly = AppDomain.CurrentDomain.Load(assemblyName);
								if (!assemblies.Contains(assembly.Location) &&
									!assemblies.Contains(Path.GetFileName(assembly.Location)))
								{
									compilerParameters.ReferencedAssemblies.Add(assembly.Location);
									assemblies.Add(assembly.Location);
								}
							}
						}
					}
				}
			}

			// Add assemblies from referencedAssemblies list
			if (ti.ReferencedAssemblies != null)
			{
				foreach (Assembly refAsm in ti.ReferencedAssemblies)
				{
					if (!assemblies.Contains(refAsm.Location) &&
						!assemblies.Contains(Path.GetFileName(refAsm.Location)))
					{
						compilerParameters.ReferencedAssemblies.Add(refAsm.Location);
					}
				}
			}

			//---------------------------
			// Compile
			//---------------------------

			string compilerVersion = "v3.5";
			if (Environment.Version.Major > 3)
			{
				compilerVersion = "v4.0";
			}

			Dictionary<string, string> providerOptions = new Dictionary<string, string>()
            {
                { "CompilerVersion", compilerVersion }
            };
			using (CodeDomProvider provider = new CSharpCodeProvider(providerOptions))
			{
				CompilerResults compilerResults = provider.CompileAssemblyFromSource(compilerParameters, ti.GeneratedSourceCode);

				if (compilerResults.Errors.HasErrors)
				{
					StringBuilder exceptionBuilder = new StringBuilder("Compilation has failed with following errors:\n============\n");

					foreach (CompilerError error in compilerResults.Errors)
					{
						exceptionBuilder.AppendLine(error.ToString());
						exceptionBuilder.AppendLine("------------------");
					}

					throw new CompileSourceException(ti, exceptionBuilder.ToString());
				}

				return compilerResults.CompiledAssembly;
			}
		}
	}
}
