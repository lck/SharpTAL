//
// ProgramMacro.cs
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

using System;
using System.Linq;
using System.Collections.Generic;

namespace SharpTAL.TemplateProgram
{
	/// <summary>
	/// The TemplateProgramMacro represents part (macro) of another TemplateProgram, and is used for the METAL implementation.
	/// The two uses for this class are:
	/// 1 - metal:define-macro results in a TemplateProgramMacro that is the macro
	/// 2 - metal:fill-slot results in a TemplateProgramMacro that is a parameter to metal:use-macro
	/// </summary>
	public class ProgramMacro : IProgram
	{
		int endTagCommandLocation;
		Program parentProgram;

		public string Name { get; private set; }
		public string TemplatePath { get { return parentProgram.TemplatePath; } }
		public string TemplateBody { get { return parentProgram.TemplateBody; } }
		public string TemplateBodyHash { get { return parentProgram.TemplateBodyHash; } }
		public Dictionary<string, IProgram> Macros { get { return parentProgram.Macros; } }
		public Dictionary<int, int> EndTagLocationTable { get { return parentProgram.EndTagLocationTable; } }
		public List<ICommand> TemplateCommands { get { return parentProgram.TemplateCommands; } }
		public int Start { get; protected set; }
		public int End { get { return parentProgram.EndTagLocationTable[endTagCommandLocation] + 1; } }

		public IEnumerable<ICommand> ProgramCommands
		{
			get
			{
				return TemplateCommands.GetRange(Start, End - Start).Where(c => c.ParentProgram == this);
			}
		}

		public Program ParentProgram
		{
			set
			{
				parentProgram = value;

				// Set the parent of each macro command
				foreach (ICommand cmd in parentProgram.TemplateCommands.GetRange(Start, End - Start))
					cmd.ParentProgram = this;
			}
		}

		/// <summary>
		/// The startRange and endRange are indexes into the parent program command list, 
		/// and defines the range of commands that we can execute
		/// </summary>
		public ProgramMacro(string name, int start, int endTagCommandLocation)
			: base()
		{
			Name = name;
			Start = start;
			this.endTagCommandLocation = endTagCommandLocation;
		}

		public override string ToString()
		{
			return string.Format("ProgramMacro {0}. Commands {1} to {2}", Name, Start, End);
		}
	}
}
