//
// CommandType.cs
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

namespace SharpTAL.TemplateProgram
{
	// TAL does not use use the order in which statements are written in the
	// tag to determine the order in which they are executed.  When an
	// element has multiple statements, they are executed in this order:
	//
	// tal:define
	// tal:condition
	// tal:repeat
	// tal:content or tal:replace
	// tal:omit-tag
	// tal:attributes
	//
	// There is a reasoning behind this ordering.  Because users often want
	// to set up variables for use in other statements contained within this
	// element or subelements, ``tal:define`` is executed first.
	// ``tal:condition`` follows, then ``tal:repeat`` , then ``tal:content``
	// or ``tal:replace``. Finally, before ``tal:attributes``, we have
	// ``tal:omit-tag`` (which is implied with ``tal:replace``).

	/// <summary>
	/// Command types (declared in order of execution).
	/// </summary>
	public enum CommandType
	{
		// META Commands
		MetaInterpolation = 1,

		// METAL Commands
		MetalUseMacro = 101,
		MetalDefineSlot = 102,
		MetalFillSlot = 103,
		MetalDefineMacro = 104,
		MetalDefineParam = 105,
		MetalFillParam = 106,
		MetalImport = 107,

		// TAL Commands
		TalDefine = 1001,
		TalCondition = 1002,
		TalRepeat = 1003,
		TalContent = 1004,
		TalReplace = 1005,
		TalAttributes = 1006,
		TalOmittag = 1007,

		// Processing Commands
		CmdStartScope = 10008,
		CmdOutput = 10009,
		CmdStartTag = 10010,
		CmdEndtagEndscope = 10011,
		CmdCodeBlock = 10012
	}
}
