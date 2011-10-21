//
// TemplateProgramMacro.cs
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

namespace SharpTAL
{
	/// <summary>
	/// The TemplateProgramMacro represents part (macro) of another TemplateProgram, and is used for the METAL implementation.
	/// The two uses for this class are:
	/// 1 - metal:define-macro results in a TemplateProgramMacro that is the macro
	/// 2 - metal:fill-slot results in a TemplateProgramMacro that is a parameter to metal:use-macro
	/// </summary>
	public class TemplateProgramMacro : TemplateProgram
	{
		int endTagSymbol;

		public string Name { get; private set; }

		public TemplateProgram ParentProgram
		{
			set
			{
				TemplateBody = value.TemplateBody;
				TemplatePath = value.TemplatePath;
				Commands = value.Commands;
				SymbolLocationTable = value.SymbolLocationTable;
				End = SymbolLocationTable[endTagSymbol] + 1;
			}
		}

		/// <summary>
		/// The startRange and endRange are indexes into the parent program command list, 
		/// and defines the range of commands that we can execute
		/// </summary>
		public TemplateProgramMacro(string name, int startRange, int endTagSymbol)
			: base()
		{
			Name = name;
			Start = startRange;
			this.endTagSymbol = endTagSymbol;
		}

		public override string ToString()
		{
			return string.Format("TemplateProgramMacro from {0} to {1}", Start, End);
		}
	}
}
