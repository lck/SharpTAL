//
// AbstractProgramInterpreter.cs
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
using System.Collections.Generic;

namespace SharpTAL.TemplateProgram
{
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
		protected abstract void Handle_TAL_ONERROR(ICommand command);
		protected abstract void Handle_CMD_START_SCOPE(ICommand command);
		protected abstract void Handle_CMD_OUTPUT(ICommand command);
		protected abstract void Handle_CMD_START_TAG(ICommand command);
		protected abstract void Handle_CMD_ENDTAG_ENDSCOPE(ICommand command);
		protected abstract void Handle_CMD_CODE_BLOCK(ICommand command);

		readonly Dictionary<CommandType, Action<ICommand>> _commandHandlers;

		protected AbstractProgramInterpreter()
		{
			_commandHandlers = new Dictionary<CommandType, Action<ICommand>>
			{
				{CommandType.MetaInterpolation, Handle_META_INTERPOLATION},
				{CommandType.MetalUseMacro, Handle_METAL_USE_MACRO},
				{CommandType.MetalDefineSlot, Handle_METAL_DEFINE_SLOT},
				{CommandType.MetalDefineParam, Handle_METAL_DEFINE_PARAM},
				{CommandType.TalDefine, Handle_TAL_DEFINE},
				{CommandType.TalCondition, Handle_TAL_CONDITION},
				{CommandType.TalRepeat, Handle_TAL_REPEAT},
				{CommandType.TalContent, Handle_TAL_CONTENT},
				{CommandType.TalReplace, Handle_TAL_REPLACE},
				{CommandType.TalAttributes, Handle_TAL_ATTRIBUTES},
				{CommandType.TalOmittag, Handle_TAL_OMITTAG},
				{CommandType.TalOnError, Handle_TAL_ONERROR},
				{CommandType.CmdStartScope, Handle_CMD_START_SCOPE},
				{CommandType.CmdOutput, Handle_CMD_OUTPUT},
				{CommandType.CmdStartTag, Handle_CMD_START_TAG},
				{CommandType.CmdEndtagEndscope, Handle_CMD_ENDTAG_ENDSCOPE},
				{CommandType.CmdCodeBlock, Handle_CMD_CODE_BLOCK}
			};
		}

		public void HandleCommands(IEnumerable<ICommand> commands)
		{
			foreach (ICommand cmd in commands)
				_commandHandlers[cmd.CommandType](cmd);
		}
	}
}
