//
// TagAttribute.cs
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

namespace SharpTAL.TemplateParser
{
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using System.Text.RegularExpressions;

	public class TagAttribute
	{
		static readonly Regex _re_needs_escape = new Regex(@"[&<>""\']");

		public string Name { get; set; }
		public string Value { get; set; }
		public string Eq { get; set; }
		public string Quote { get; set; }
		public string QuoteEntity { get; set; }
		
		public string EscapedValue
		{
			get
			{
				string str = Value;

				if (string.IsNullOrEmpty(str))
					return str;

				if (!_re_needs_escape.IsMatch(str))
					return str;

				if (str.IndexOf('&') >= 0)
					str = str.Replace("&", "&amp;");

				if (str.IndexOf('>') >= 0)
					str = str.Replace("<", "&lt;");

				if (str.IndexOf('>') >= 0)
					str = str.Replace(">", "&gt;");

				if (!string.IsNullOrEmpty(Quote) && str.IndexOf(Quote) >= 0)
					str = str.Replace(Quote, QuoteEntity);

				return str;
			}
		}

		public string UnescapedValue
		{
			get
			{
				string str = Value;

				if (string.IsNullOrEmpty(str))
					return str;

				int cp = HTMLEntityDefs.Name2Code["lt"];
				str = str.Replace("&lt;", ((char)cp).ToString());

				cp = HTMLEntityDefs.Name2Code["gt"];
				str = str.Replace("&gt;", ((char)cp).ToString());

				cp = HTMLEntityDefs.Name2Code["quot"];
				str = str.Replace("&quot;", ((char)cp).ToString());

				return str;
			}
		}

		public override string ToString()
		{
			return string.Format(" {0}{1}{2}{3}{2}", Name, Eq, Quote, EscapedValue);
		}
	}
}
