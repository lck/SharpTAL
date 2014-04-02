//
// TemplateParseException.cs
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
using SharpTAL.TemplateParser;

namespace SharpTAL
{
	public class TemplateParseException : Exception
	{
		private readonly Tag _tag;
		private readonly string _errorDescription;

		public TemplateParseException(Tag tag, string errorDescription)
			: base(errorDescription)
		{
			_tag = tag;
			_errorDescription = errorDescription;
		}

		public override string Message
		{
			get
			{
				if (_tag != null)
				{
					return string.Format("{1}{0}Tag: {2}{0}Source: {3}{0}Line: {4}{0}Position: {5}",
						Environment.NewLine, _errorDescription,
						_tag, _tag.SourcePath, _tag.LineNumber, _tag.LinePosition);
				}
				return _errorDescription;
			}
		}

		public override string ToString()
		{
			return Message;
		}
	}
}
