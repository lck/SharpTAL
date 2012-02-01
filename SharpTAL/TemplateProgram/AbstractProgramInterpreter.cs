//
// AbstractProgramInterpreter.cs
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

namespace SharpTAL.TemplateProgram
{
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using SharpTAL.TemplateParser;
	using SharpTAL.TemplateProgram.Commands;

	public abstract class AbstractProgramInterpreter
	{
		protected abstract void Handle_META_INTERPOLATION(ICommand command);
		protected abstract void Handle_METAL_USE_MACRO(ICommand command);
		protected abstract void Handle_METAL_DEFINE_SLOT(ICommand command);
		protected abstract void Handle_METAL_FILL_SLOT(ICommand command);
		protected abstract void Handle_METAL_DEFINE_MACRO(ICommand command);
		protected abstract void Handle_METAL_DEFINE_PARAM(ICommand command);
		protected abstract void Handle_METAL_FILL_PARAM(ICommand command);
		protected abstract void Handle_METAL_IMPORT(ICommand command);
		protected abstract void Handle_TAL_DEFINE(ICommand command);
		protected abstract void Handle_TAL_CONDITION(ICommand command);
		protected abstract void Handle_TAL_REPEAT(ICommand command);
		protected abstract void Handle_TAL_CONTENT(ICommand command);
		protected abstract void Handle_TAL_REPLACE(ICommand command);
		protected abstract void Handle_TAL_ATTRIBUTES(ICommand command);
		protected abstract void Handle_TAL_OMITTAG(ICommand command);
		protected abstract void Handle_CMD_START_SCOPE(ICommand command);
		protected abstract void Handle_CMD_OUTPUT(ICommand command);
		protected abstract void Handle_CMD_START_TAG(ICommand command);
		protected abstract void Handle_CMD_ENDTAG_ENDSCOPE(ICommand command);
		
		Dictionary<CommandType, Action<ICommand>> commandHandlers;

		public AbstractProgramInterpreter()
		{
			commandHandlers = new Dictionary<CommandType, Action<ICommand>>();
			commandHandlers.Add(CommandType.META_INTERPOLATION, Handle_META_INTERPOLATION);
			commandHandlers.Add(CommandType.METAL_USE_MACRO, Handle_METAL_USE_MACRO);
			commandHandlers.Add(CommandType.METAL_DEFINE_SLOT, Handle_METAL_DEFINE_SLOT);
			commandHandlers.Add(CommandType.METAL_DEFINE_PARAM, Handle_METAL_DEFINE_PARAM);
			commandHandlers.Add(CommandType.TAL_DEFINE, Handle_TAL_DEFINE);
			commandHandlers.Add(CommandType.TAL_CONDITION, Handle_TAL_CONDITION);
			commandHandlers.Add(CommandType.TAL_REPEAT, Handle_TAL_REPEAT);
			commandHandlers.Add(CommandType.TAL_CONTENT, Handle_TAL_CONTENT);
			commandHandlers.Add(CommandType.TAL_REPLACE, Handle_TAL_REPLACE);
			commandHandlers.Add(CommandType.TAL_ATTRIBUTES, Handle_TAL_ATTRIBUTES);
			commandHandlers.Add(CommandType.TAL_OMITTAG, Handle_TAL_OMITTAG);
			commandHandlers.Add(CommandType.CMD_START_SCOPE, Handle_CMD_START_SCOPE);
			commandHandlers.Add(CommandType.CMD_OUTPUT, Handle_CMD_OUTPUT);
			commandHandlers.Add(CommandType.CMD_START_TAG, Handle_CMD_START_TAG);
			commandHandlers.Add(CommandType.CMD_ENDTAG_ENDSCOPE, Handle_CMD_ENDTAG_ENDSCOPE);
		}

		public void HandleCommands(IEnumerable<ICommand> commands)
		{
			foreach (ICommand cmd in commands)
				commandHandlers[cmd.CommandType](cmd);
		}
	}
}
