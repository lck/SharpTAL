//
// Program.cs
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

namespace SharpTAL.TemplateProgram
{
	using System;
	using System.Linq;
	using System.Collections.Generic;

	using SharpTAL.TemplateProgram.Commands;

	public class Program : IProgram
	{
		protected List<ICommand> templateCommands;

		public string Name { get { return string.Empty; } }
		public string TemplatePath { get; protected set; }
		public string TemplateBody { get; protected set; }
		public string TemplateBodyHash { get; protected set; }
		public Dictionary<string, IProgram> Macros { get; protected set; }
		public Dictionary<int, int> EndTagLocationTable { get; protected set; }
		public int Start { get; protected set; }
		public int End { get; protected set; }

		public List<ICommand> TemplateCommands
		{
			get
			{
				return templateCommands;
			}
			protected set
			{
				templateCommands = value;
			}
		}

		public IEnumerable<ICommand> ProgramCommands
		{
			get
			{
				return TemplateCommands.GetRange(Start, End).Where(c => c.ParentProgram == this);
			}
		}

		/// <summary>
		/// Metal import:macro commands cache
		/// </summary>
		public HashSet<string> ImportMacroCommands { get; protected set; }

		public Program()
		{
			Start = 0;
			End = 0;
		}

		public Program(string templateBody, string templatePath, string templateHash, List<ICommand> templateCommands, Dictionary<int, int> endTagLocationTable, Dictionary<string, IProgram> macros, HashSet<string> importMacroCommands)
		{
			TemplateBody = templateBody;
			TemplatePath = templatePath;
			TemplateBodyHash = templateHash;
			TemplateCommands = templateCommands;
			Macros = macros;
			EndTagLocationTable = endTagLocationTable;
			ImportMacroCommands = importMacroCommands;
			Start = 0;

			if (ProgramCommands != null)
			{
				End = templateCommands.Count;

				// Set the parent of each command
				foreach (ICommand cmd in TemplateCommands)
					cmd.ParentProgram = this;

				// Set the parent of each macro
				if (Macros != null)
				{
					foreach (ProgramMacro macro in Macros.Values)
						macro.ParentProgram = this;
				}

				// Set the parent of each slot
				foreach (METALUseMacro useMacroCmd in TemplateCommands.Where(c => c.CommandType == CommandType.METAL_USE_MACRO))
				{
					foreach (ProgramSlot slot in useMacroCmd.Slots.Values)
						slot.ParentProgram = this;
				}
			}
		}
	}
}
