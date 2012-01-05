//
// Element.cs
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
using System.Linq;

namespace SharpTAL.TemplateParser
{
	public class Element
	{
		public ElementKind Kind { get; private set; }
		public Dictionary<string, object> StartTagTokens { get; private set; }
		public Dictionary<string, object> EndTagTokens { get; private set; }
		public List<Element> Children { get; private set; }

		public Element(ElementKind kind, Token startTag)
		{
			Kind = kind;
			StartTagTokens = new Dictionary<string, object> { { "", startTag } };
			EndTagTokens = new Dictionary<string, object>();
			Children = new List<Element>();
		}

		public Element(ElementKind kind, Dictionary<string, object> startTagTokens)
		{
			Kind = kind;
			StartTagTokens = startTagTokens;
			EndTagTokens = new Dictionary<string, object>();
			Children = new List<Element>();
		}

		public Element(ElementKind kind, Dictionary<string, object> startTagTokens, Dictionary<string, object> endTagTokens, List<Element> children)
		{
			Kind = kind;
			StartTagTokens = startTagTokens;
			EndTagTokens = endTagTokens;
			Children = children;
		}
	}
}
