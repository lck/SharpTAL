//
// Demo.cs
//
// Author:
//   Roman Lacko (backup.rlacko@gmail.com)
//
// Copyright (c) 2010 - 2011 Roman Lacko
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
using System.Text;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Diagnostics;

namespace SharpTAL.Demo
{
	public static class DemoExtensions
	{
		public static string XmlToString(this XmlDocument xml)
		{
			StringBuilder sb = new StringBuilder();
			StringWriter sw = new StringWriter(sb);
			xml.Save(sw);
			return sb.ToString();
		}

		public static string ToUpperExtension(this string s)
		{
			return s.ToUpper();
		}
	}

	public class Friend
	{
		public string Name;
		public int Age;
	}

	class Demo
	{
		static void Main(string[] args)
		{
			TemplateInfo ti;
			try
			{
				// Referenced Assemblies
				List<Assembly> refAssemblies = new List<Assembly>() { typeof(Demo).Assembly };

				// Globals
				Dictionary<string, object> globals = new Dictionary<string, object>()
                {
                    {
						"friends", new List<Friend>()
						{
							new Friend() { Name="Samantha", Age=33 },
							new Friend() { Name="Kim", Age=35 },
							new Friend() { Name="Sandra", Age=22 },
							new Friend() { Name="Natalie", Age=20 }
						}
					}
                };
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.LoadXml(Resources.Macros);
				globals.Add("xmlDoc", xmlDoc);

				// Inline templates
				Dictionary<string, string> inlineTemplates = new Dictionary<string, string>();
				inlineTemplates.Add("Macros", Resources.Macros);

				// Speed tests
				Console.WriteLine("-------------------------------");
				Console.WriteLine("Speed tests:");
				Console.WriteLine("-------------------------------");
				Stopwatch sw = new Stopwatch();
				sw.Start();
				int count = 200;
				for (int i = 0; i < count; i++)
				{
					TALCompiler.CompileTemplate(new TemplateInfo() { TemplateBody = Resources.Main, InlineTemplates = inlineTemplates });
				}
				sw.Stop();
				Console.WriteLine(string.Format("Precompile {0} templates: {1} milliseconds", count, sw.ElapsedMilliseconds));
				Console.WriteLine();

				// Template cache no. 1
				Console.WriteLine("-------------------------------");
				Console.WriteLine("[1] Initializing template cache:");
				Console.WriteLine("-------------------------------");
				Console.WriteLine();

				string cacheFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Template Cache");
				Console.WriteLine(string.Format("Cache folder: {0}", cacheFolder));
				if (!Directory.Exists(cacheFolder))
				{
					Directory.CreateDirectory(cacheFolder);
				}
				FileSystemTemplateCache cache1 = new FileSystemTemplateCache(cacheFolder, true, @"Demo_{key}.dll");

				// Template rendering no. 1
				Console.WriteLine("-------------------------------");
				Console.WriteLine("[1] Rendering template Main:");
				Console.WriteLine("-------------------------------");
				Console.WriteLine(Resources.Main);
				Console.WriteLine();

				for (int i = 0; i < 2; i++)
				{
					sw.Reset();
					sw.Start();
					string result = cache1.RenderTemplate(Resources.Main, globals, inlineTemplates, refAssemblies, out ti);
					sw.Stop();

					Console.WriteLine("-------------------------------");
					Console.WriteLine(string.Format("[1] Result ({0}. Milliseconds: {1}):", i + 1, sw.ElapsedMilliseconds));
					Console.WriteLine("-------------------------------");
					Console.WriteLine(result);
				}

				// Template cache no. 2
				Console.WriteLine("-------------------------------");
				Console.WriteLine("[2] Initializing template cache (reusing templates):");
				Console.WriteLine("-------------------------------");
				FileSystemTemplateCache cache2 = new FileSystemTemplateCache(cacheFolder, false, @"Demo_{key}.dll");

				// Template rendering no. 2
				for (int i = 0; i < 2; i++)
				{
					sw.Reset();
					sw.Start();
					string result = cache2.RenderTemplate(Resources.Main, globals, inlineTemplates, refAssemblies, out ti);
					sw.Stop();

					Console.WriteLine("-------------------------------");
					Console.WriteLine(string.Format("[2] Result ({0}. Milliseconds: {1}):", i + 1, sw.ElapsedMilliseconds));
					Console.WriteLine("-------------------------------");
					Console.WriteLine(result);
				}

				// In-memory Template cache
				Console.WriteLine("--------------------------------------------");
				Console.WriteLine("[mem] Initializing in-memory template cache:");
				Console.WriteLine("--------------------------------------------");
				MemoryTemplateCache cache3 = new MemoryTemplateCache();

				// In-memory Template rendering
				for (int i = 0; i < 2; i++)
				{
					sw.Reset();
					sw.Start();
					string result = cache3.RenderTemplate(Resources.Main, globals, inlineTemplates, refAssemblies, out ti);
					sw.Stop();

					Console.WriteLine("-------------------------------");
					Console.WriteLine(string.Format("[mem] Result ({0}. Milliseconds: {1}):", i + 1, sw.ElapsedMilliseconds));
					Console.WriteLine("-------------------------------");
					Console.WriteLine(result);
				}
			}
			catch (TemplateParseException ex)
			{
				Console.WriteLine("");
				Console.WriteLine("-------------------------------");
				Console.WriteLine(ex.Message);
				Console.WriteLine("-------------------------------");
			}
			catch (CompileSourceException ex)
			{
				Console.WriteLine("");
				Console.WriteLine("-------------------------------");
				Console.WriteLine(ex.Message);
				Console.WriteLine("-------------------------------");
			}
			catch (RenderTemplateException ex)
			{
				Console.WriteLine("");
				Console.WriteLine("-------------------------------");
				Console.WriteLine(ex.Message);
				Console.WriteLine("-------------------------------");
			}
			catch (Exception ex)
			{
				Console.WriteLine("");
				Console.WriteLine("-------------------------------");
				Console.WriteLine(ex.Message);
				Console.WriteLine("-------------------------------");
			}

			Console.WriteLine("");
			Console.WriteLine("Press any key ...");
			Console.ReadKey();
		}
	}
}
