//
// Token.cs
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

using System.Linq;

namespace SharpTAL.TemplateParser
{
	public class Token
	{
		private readonly string _token;
		private readonly int _position;
		private readonly string _source;
		private readonly string _filename;

		public Token(string token, int position = 0, string source = null, string filename = null)
		{
			_token = token;
			_position = position;
			_source = source;
			_filename = filename ?? "";
		}

		public override bool Equals(object obj)
		{
			if (obj is Token)
				return _token == (obj as Token)._token;
			return false;
		}

		public override string ToString()
		{
			return _token;
		}

		public TokenKind Kind
		{
			get
			{
				if (_token.StartsWith("<"))
				{
					if (_token.StartsWith("<!--"))
						return TokenKind.Comment;
					if (_token.StartsWith("<![CDATA["))
						return TokenKind.CData;
					if (_token.StartsWith("<!"))
						return TokenKind.Declaration;
					if (_token.StartsWith("<?xml"))
						return TokenKind.XmlDeclaration;
					if (_token.StartsWith("<?"))
						return TokenKind.ProcessingInstruction;
					if (_token.StartsWith("</"))
						return TokenKind.EndTag;
					if (_token.EndsWith("/>"))
						return TokenKind.EmptyTag;
					if (_token.EndsWith(">"))
						return TokenKind.StartTag;
					return TokenKind.Invalid;
				}
				return TokenKind.Text;
			}
		}

		public int Position
		{
			get { return _position; }
		}

		public string Filename
		{
			get { return _filename; }
		}

		public Location Location
		{
			get
			{
				if (string.IsNullOrEmpty(_source))
					return new Location(0, _position);

				string body = _source.Substring(0, _position);
				int line = body.Count(c => c == '\n');
				return new Location(line + 1, _position - body.LastIndexOf('\n') - 1);
			}
		}

		public Token Substring(int start)
		{
			return new Token(_token.Substring(start), _position + start, _source, _filename);
		}

		public Token Substring(int start, int end)
		{
			return new Token(_token.Substring(start, end), _position + start, _source, _filename);
		}
	}
}
