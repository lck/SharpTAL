//
// IProgram.cs
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

namespace SharpTAL.TemplateProgram
{
	using System;
	using System.Linq;
	using System.Collections.Generic;

	public interface IProgram
	{
		/// <summary>
		/// Name of macro
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Source template path
		/// </summary>
		string TemplatePath { get; }

		/// <summary>
		/// Source template body
		/// </summary>
		string TemplateBody { get; }

		/// <summary>
		/// Source template body hash
		/// </summary>
		string TemplateBodyHash { get; }

		/// <summary>
		/// End tags locations table
		/// </summary>
		Dictionary<int, int> EndTagLocationTable { get; }

		/// <summary>
		/// Commands of the entite template
		/// </summary>
		List<ICommand> TemplateCommands { get; }
		
		/// <summary>
		/// Commands of this program
		/// </summary>
		IEnumerable<ICommand> ProgramCommands { get; }

		/// <summary>
		/// Index of the first program command
		/// </summary>
		int Start { get; }

		/// <summary>
		/// Index of the last program command
		/// </summary>
		int End { get; }
		
		/// <summary>
		/// Program macros
		/// </summary>
		Dictionary<string, IProgram> Macros { get; }
	}
}
