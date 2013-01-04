//
// ITALESIterator.cs
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

namespace SharpTAL
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	/// <summary>
	/// TAL Iterator provided by TALES
	///
	/// Values of this iterator are assigned to items in the repeat namespace.
	///
	/// For example, with a TAL statement like: tal:repeat="item items",
	/// an iterator will be assigned to "repeat/item".  The iterator
	/// provides a number of handy methods useful in writing TAL loops.
	///
	/// The results are undefined of calling any of the methods except
	/// 'length' before the first iteration.
	/// </summary>
	public interface ITALESIterator
	{
		void next(bool isLast);
		
		int length { get; }

		int index { get; }

		int number { get; }

		bool even { get; }

		bool odd { get; }

		bool start { get; }

		bool end { get; }

		string letter { get; }

		string Letter { get; }

		string roman { get; }

		string Roman { get; }
	}
}
