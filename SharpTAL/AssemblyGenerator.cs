//
// AssemblyGenerator.cs
//
// Author:
//   Roman Lacko (backup.rlacko@gmail.com)
//
// Copyright (c) 2010 - 2013 Roman Lacko
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
using System.Text;
using System.CodeDom.Compiler;
using Microsoft.CSharp;

namespace SharpTAL
{
	public class AssemblyGenerator
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
                "System.Security.dll",
				typeof(Template).Assembly.Location
            };
			foreach (string asmName in assemblies)
				compilerParameters.ReferencedAssemblies.Add(asmName);

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
					string[] sourceLines = ti.GeneratedSourceCode.Split('\n');

					foreach (CompilerError error in compilerResults.Errors)
					{
						exceptionBuilder.AppendLine(error.ErrorNumber + ": " + error.ErrorText + " (line " + error.Line.ToString() + ")");

						// Add source lines for context
						int firstLine = Math.Max(0, error.Line - 2);
						int secondLine = Math.Min(sourceLines.Length, error.Line + 2);
						for (int i = firstLine; i <= secondLine; i++)
						{
							exceptionBuilder.AppendLine(sourceLines[i].Trim());
						}

						exceptionBuilder.AppendLine("------------------");
					}

					throw new CompileSourceException(ti, compilerResults.Errors, exceptionBuilder.ToString());
				}

				return compilerResults.CompiledAssembly;
			}
		}
	}
}
