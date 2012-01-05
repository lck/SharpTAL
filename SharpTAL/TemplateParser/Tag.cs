//
// Tag.cs
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

namespace SharpTAL.TemplateParser
{
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using System.Text.RegularExpressions;

	public class Tag
	{
		public string Name { get; set; }
		public string Suffix { get; set; }
		public bool Singleton { get; set; }
		public List<TagAttribute> Attributes { get; set; }
		public int LineNumber { get; set; }
		public int LinePosition { get; set; }
		public string SourcePath { get; set; }

		public Tag()
		{
		}

		public Tag(Tag tag)
		{
			Name = tag.Name;
			Suffix = tag.Suffix;
			Singleton = tag.Singleton;
			if (tag.Attributes != null)
				Attributes = new List<TagAttribute>(tag.Attributes);
			SourcePath = tag.SourcePath;
			LineNumber = tag.LineNumber;
			LinePosition = tag.LinePosition;
		}

		public string Format()
		{
			string result = "<";
			result += Name;
			if (Attributes != null)
			{
				foreach (var att in Attributes)
				{
					result += string.Format(" {0}{1}{2}{3}{2}", att.Name, att.Eq, att.Quote, att.EscapedValue);
				}
			}
			result += Suffix;
			return result;
		}

		public override string ToString()
		{
			return Format();
		}
	}
}
