//
// CommandType.cs
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
		META_INTERPOLATION = 1,

		// METAL Commands
		METAL_USE_MACRO = 101,
		METAL_DEFINE_SLOT = 102,
		METAL_FILL_SLOT = 103,
		METAL_DEFINE_MACRO = 104,
		METAL_DEFINE_PARAM = 105,
		METAL_FILL_PARAM = 106,
		METAL_IMPORT = 107,

		// TAL Commands
		TAL_DEFINE = 1001,
		TAL_CONDITION = 1002,
		TAL_REPEAT = 1003,
		TAL_CONTENT = 1004,
		TAL_REPLACE = 1005,
		TAL_ATTRIBUTES = 1006,
		TAL_OMITTAG = 1007,
		
		// Processing Commands
		CMD_START_SCOPE = 10008,
		CMD_OUTPUT = 10009,
		CMD_START_TAG = 10010,
		CMD_ENDTAG_ENDSCOPE = 10011,
		CMD_CODE_BLOCK = 10012
	}
}
