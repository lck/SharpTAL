//
// Template.cs
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
using System.Reflection;
using System.IO;
using System.Globalization;

namespace SharpTAL
{
	public class Template
	{
		private static ITemplateCache defaultTemplateCache = new MemoryTemplateCache();
		protected string body;
		protected Dictionary<string, Type> globalsTypes;
		protected List<Assembly> referencedAssemblies;
		protected ITemplateCache templateCache;
		protected TemplateInfo templateInfo;

		public CultureInfo Culture { get; set; }

		public ITemplateCache TemplateCache
		{
			set
			{
				templateInfo = null;
				templateCache = value;
			}
			get
			{
				return templateCache ?? defaultTemplateCache;
			}
		}

		public Template(string body) :
			this(body, null, null)
		{
		}

		public Template(string body, Dictionary<string, Type> globalsTypes) :
			this(body, globalsTypes, null)
		{
		}

		public Template(string body, Dictionary<string, Type> globalsTypes, List<Assembly> referencedAssemblies)
		{
			this.body = body;
			this.globalsTypes = globalsTypes;
			this.referencedAssemblies = referencedAssemblies;
			this.templateCache = defaultTemplateCache;
			this.Culture = CultureInfo.InvariantCulture;
		}

		public void Compile()
		{
			templateInfo = templateCache.CompileTemplate(body, globalsTypes, referencedAssemblies);
		}

		public string Render(Dictionary<string, object> globals)
		{
			using (MemoryStream outputStream = new MemoryStream())
			{
				using (StreamWriter outputWriter = new StreamWriter(outputStream))
				{
					Render(outputWriter, globals);
					outputWriter.Flush();
					outputStream.Position = 0;
					StreamReader reader = new StreamReader(outputStream);
					string result = reader.ReadToEnd();
					return result;
				}
			}
		}

		public void Render(StreamWriter outputWriter, Dictionary<string, object> globals)
		{
			CheckRenderInput(globals);
			try
			{
				templateInfo.RenderMethod.Invoke(null, new object[] { outputWriter, globals, Culture });
			}
			catch (TargetInvocationException ex)
			{
				throw new RenderTemplateException(templateInfo, ex.InnerException.Message, ex.InnerException);
			}
			catch (Exception ex)
			{
				throw new RenderTemplateException(templateInfo, ex.Message, ex);
			}
		}

		void CheckRenderInput(Dictionary<string, object> globals)
		{
			if (templateInfo == null)
			{
				if (globalsTypes == null)
				{
					Recompile(globals);
					return;
				}
				else
				{
					Compile();
					return;
				}
			}
			if (globals.Count != globalsTypes.Count)
			{
				Recompile(globals);
				return;
			}
			foreach (string name in globalsTypes.Keys)
			{
				if (!globals.ContainsKey(name))
				{
					Recompile(globals);
					return;
				}
				if (globals[name].GetType() != globalsTypes[name])
				{
					Recompile(globals);
					return;
				}
			}
		}

		void Recompile(Dictionary<string, object> globals)
		{
			globalsTypes = new Dictionary<string, Type>();
			foreach (string name in globals.Keys)
			{
				object obj = globals[name];
				globalsTypes.Add(name, obj != null ? obj.GetType() : null);
			}
			templateInfo = templateCache.CompileTemplate(body, globalsTypes, referencedAssemblies);
		}
	}
}
