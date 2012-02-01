//
// Token.cs
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpTAL.TemplateParser
{
	public class Token
	{
		string token;
		int position = 0;
		string source = null;
		string filename = null;

		public Token(string token, int position = 0, string source = null, string filename = null)
		{
			this.token = token;
			this.position = position;
			this.source = source;
			this.filename = filename ?? "";
		}

		public override bool Equals(object obj)
		{
			if (obj is Token)
				return token == (obj as Token).token;
			return false;
		}

		public override string ToString()
		{
			return token;
		}

		public TokenKind Kind
		{
			get
			{
				if (token.StartsWith("<"))
				{
					if (token.StartsWith("<!--"))
						return TokenKind.Comment;
					if (token.StartsWith("<![CDATA["))
						return TokenKind.CData;
					if (token.StartsWith("<!"))
						return TokenKind.Declaration;
					if (token.StartsWith("<?xml"))
						return TokenKind.XmlDeclaration;
					if (token.StartsWith("<?"))
						return TokenKind.ProcessingInstruction;
					if (token.StartsWith("</"))
						return TokenKind.EndTag;
					if (token.EndsWith("/>"))
						return TokenKind.EmptyTag;
					if (token.EndsWith(">"))
						return TokenKind.StartTag;
					return TokenKind.Invalid;
				}
				return TokenKind.Text;
			}
		}

		public int Position
		{
			get { return position; }
		}

		public string Filename
		{
			get { return filename; }
		}

		public Location Location
		{
			get
			{
				if (string.IsNullOrEmpty(this.source))
					return new Location(0, this.position);

				string body = this.source.Substring(0, this.position);
				int line = body.Count(c => c == '\n');
				return new Location(line + 1, this.position - body.LastIndexOf('\n') - 1);
			}
		}

		public Token Substring(int start)
		{
			return new Token(token.Substring(start), position + start, source, filename);
		}

		public Token Substring(int start, int end)
		{
			return new Token(token.Substring(start, end), position + start, source, filename);
		}
	}
}
