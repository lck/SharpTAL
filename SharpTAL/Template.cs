//
// Template.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Globalization;

using SharpTAL.TemplateCache;

namespace SharpTAL
{
	public class Template : ITemplate
	{
		private static readonly ITemplateCache DefaultTemplateCache = new MemoryTemplateCache();
		private readonly string _body;
		private Dictionary<string, Type> _globalsTypes;
		private readonly List<Assembly> _referencedAssemblies;
		private ITemplateCache _templateCache;
		private TemplateInfo _templateInfo;

		public CultureInfo Culture { get; set; }

		public ITemplateCache TemplateCache
		{
			set
			{
				_templateInfo = null;
				_templateCache = value;
			}
			get
			{
				return _templateCache ?? DefaultTemplateCache;
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

		public Template(string body, List<Assembly> referencedAssemblies) :
			this(body, null, referencedAssemblies)
		{
		}

		public Template(string body, Dictionary<string, Type> globalsTypes, List<Assembly> referencedAssemblies)
		{
			_body = body;
			_globalsTypes = globalsTypes;
			_referencedAssemblies = referencedAssemblies;
			_templateCache = DefaultTemplateCache;
			Culture = CultureInfo.InvariantCulture;
		}

		#region ITemplate interface implementation

		public void Compile()
		{
			Recompile(null);
		}

		public string Render(Dictionary<string, object> globals)
		{
			using (var outputStream = new MemoryStream())
			using (var outputWriter = new StreamWriter(outputStream))
			{
				Render(outputWriter, globals);
				outputWriter.Flush();
				outputStream.Position = 0;
				var reader = new StreamReader(outputStream);
				string result = reader.ReadToEnd();
				return result;
			}
		}

		public void Render(StreamWriter outputWriter, Dictionary<string, object> globals)
		{
			CompileCheck(globals);

			try
			{
				IRenderContext context = new RenderContext();

				foreach (var item in globals)
					context[item.Key] = item.Value;

				context["__FormatResult"] = new Func<object, string>(FormatResult);
				context["__IsFalseResult"] = new Func<object, bool>(IsFalseResult);

				if (!context.ContainsKey("repeat"))
					context["repeat"] = new RepeatDictionary();

				_templateInfo.RenderMethod.Invoke(null, new object[] { outputWriter, context });
			}
			catch (TargetInvocationException ex)
			{
				throw new RenderTemplateException(_templateInfo, ex.InnerException.Message, ex.InnerException);
			}
			catch (Exception ex)
			{
				throw new RenderTemplateException(_templateInfo, ex.Message, ex);
			}
		}

		#endregion

		protected virtual string DefaultExpressionType
		{
			get { return "csharp"; }
		}

		protected virtual string FormatResult(object result)
		{
			var formattable = result as IFormattable;
			string resultValue;
			if (formattable != null)
				resultValue = formattable.ToString("", Culture);
			else
				resultValue = result.ToString();
			return resultValue;
		}

		protected virtual bool IsFalseResult(object obj)
		{
			if (obj == null)
			{
				// Value was Nothing
				return true;
			}
			if (obj is bool)
			{
				return ((bool)obj) == false;
			}
			if (obj is int)
			{
				return ((int)obj) == 0;
			}
			if (obj is float)
			{
				return ((float)obj) == 0;
			}
			if (obj is double)
			{
				return ((double)obj) == 0;
			}
			if (obj is string)
			{
				return string.IsNullOrEmpty(((string)obj));
			}
			if (obj is IEnumerable)
			{
				return ((IEnumerable)obj).GetEnumerator().MoveNext() == false;
			}
			// Everything else is true, so we return false!
			return false;
		}

		void CompileCheck(Dictionary<string, object> globals)
		{
			// First time compile
			if (_templateInfo == null)
			{
				Recompile(globals);
				return;
			}

			if (globals == null && _globalsTypes == null)
				return;

			// Compare globals and globalsTypes
			if ((globals == null && _globalsTypes != null) ||
				(globals != null && _globalsTypes == null) ||
				(globals != null && _globalsTypes != null && globals.Count != _globalsTypes.Count))
			{
				Recompile(globals);
				return;
			}

			foreach (string varName in _globalsTypes.Keys)
			{
				if (globals.ContainsKey(varName) == false ||
					globals[varName].GetType() != _globalsTypes[varName])
				{
					Recompile(globals);
					return;
				}
			}
		}

		void Recompile(Dictionary<string, object> globals)
		{
			if (globals != null && globals.Count > 0)
			{
				_globalsTypes = new Dictionary<string, Type>();
				foreach (string name in globals.Keys)
				{
					object obj = globals[name];
					_globalsTypes.Add(name, obj != null ? obj.GetType() : null);
				}
			}
			_templateInfo = _templateCache.CompileTemplate(_body, _globalsTypes, _referencedAssemblies);
		}
	}
}
