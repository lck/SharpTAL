//
// TemplateProgram.cs
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
using System.Linq;
using System.Collections.Generic;

namespace SharpTAL
{
	public class TemplateProgram
	{
		/// <summary>
		/// Source template path
		/// </summary>
		public string TemplatePath { get; protected set; }

		/// <summary>
		/// Source template body
		/// </summary>
		public string TemplateBody { get; protected set; }

		/// <summary>
		/// Source template body hash
		/// </summary>
		public string TemplateBodyHash { get; protected set; }

		/// <summary>
		/// Program commands
		/// </summary>
		public List<Command> Commands { get; protected set; }

		/// <summary>
		/// Program symbol location table
		/// </summary>
		public Dictionary<int, int> SymbolLocationTable { get; protected set; }

		/// <summary>
		/// Program macros
		/// </summary>
		public Dictionary<string, TemplateProgramMacro> Macros { get; protected set; }

		/// <summary>
		/// Metal import:macro commands cache
		/// </summary>
		public HashSet<string> ImportCommands { get; protected set; }

		public virtual int Start { get; protected set; }

		public virtual int End { get; protected set; }

		public TemplateProgram()
		{
			Start = 0;
			End = 0;
		}

		public TemplateProgram(string templateBody, string templatePath, string templateHash, List<Command> commands, Dictionary<int, int> symbolLocationTable, Dictionary<string, TemplateProgramMacro> macros, HashSet<string> importCommands)
		{
			TemplateBody = templateBody;
			TemplatePath = templatePath;
			TemplateBodyHash = templateHash;
			Commands = commands;
			SymbolLocationTable = symbolLocationTable;
			Macros = macros;
			ImportCommands = importCommands;
			Start = 0;
			if (Macros != null)
			{
				// Set the parent of each macro
				foreach (TemplateProgramMacro macro in Macros.Values)
				{
					macro.ParentProgram = this;
				}
			}
			if (Commands != null)
			{
				End = Commands.Count;

				// Set the parent of each slot
				foreach (Command useMacroCmd in Commands.Where(c => c.CommandType == CommandType.METAL_USE_MACRO))
				{
					Dictionary<string, TemplateProgramMacro> slotMap = (Dictionary<string, TemplateProgramMacro>)useMacroCmd.Attributes[1];
					foreach (TemplateProgramMacro slot in slotMap.Values)
					{
						slot.ParentProgram = this;
					}
				}
			}
		}
	}
}
