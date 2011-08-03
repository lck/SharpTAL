//
// Token.cs
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
using System.Text;

namespace SharpTAL.Parser
{
	public class Token
	{
		string _str;
		int _pos = 0;
		string _source = null;
		string _filename = null;

		public Token()
		{
		}

		public Token(string str, int pos = 0, string source = null, string filename = null)
		{
			this._str = str;
			this._pos = pos;
			this._source = source;
			this._filename = filename ?? "";
		}

		public override bool Equals(object obj)
		{
			if (obj is Token)
				return _str == (obj as Token)._str;
			return false;
		}
		
		public override string ToString()
		{
			return _str;
		}

		public int Pos
		{
			get { return _pos; }
		}

		public string Filename
		{
			get { return _filename; }
		}

		public Location Location
		{
			get
			{
				if (string.IsNullOrEmpty(this._source))
					return new Location(0, this._pos);

				string body = this._source.Substring(0, this._pos);
				int line = body.Count(c => c == '\n');
				return new Location(line + 1, this._pos - body.LastIndexOf('\n') - 1);
			}
		}

		public Token SubString(int start, int end)
		{
			if (end == -1)
				return new Token(_str.Substring(start), _pos + start, _source, _filename);
			return new Token(_str.Substring(start, end), _pos + start, _source, _filename);
		}
	}
}
