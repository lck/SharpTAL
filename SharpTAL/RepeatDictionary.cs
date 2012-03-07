//
// RepeatDictionary.cs
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

namespace SharpTAL
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	/// <summary>
	/// Repeat dictionary implementation
	/// </summary>
	public class RepeatDictionary : IRepeatDictionary
	{
		IDictionary<string, ITALESIterator> dict;

		public RepeatDictionary()
		{
			dict = new Dictionary<string, ITALESIterator>();
		}

		public void Add(string key, ITALESIterator value)
		{
			dict.Add(key, value);
		}

		public bool ContainsKey(string key)
		{
			return dict.ContainsKey(key);
		}

		public ICollection<string> Keys
		{
			get { return dict.Keys; }
		}

		public bool Remove(string key)
		{
			return dict.Remove(key);
		}

		public bool TryGetValue(string key, out ITALESIterator value)
		{
			return dict.TryGetValue(key, out value);
		}

		public ICollection<ITALESIterator> Values
		{
			get { return dict.Values; }
		}

		public ITALESIterator this[string key]
		{
			get
			{
				return dict[key];
			}
			set
			{
				dict[key] = value;
			}
		}

		public void Add(KeyValuePair<string, ITALESIterator> item)
		{
			dict.Add(item);
		}

		public void Clear()
		{
			dict.Clear();
		}

		public bool Contains(KeyValuePair<string, ITALESIterator> item)
		{
			return dict.Contains(item);
		}

		public void CopyTo(KeyValuePair<string, ITALESIterator>[] array, int arrayIndex)
		{
			dict.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return dict.Count; }
		}

		public bool IsReadOnly
		{
			get { return dict.IsReadOnly; }
		}

		public bool Remove(KeyValuePair<string, ITALESIterator> item)
		{
			return dict.Remove(item);
		}

		public IEnumerator<KeyValuePair<string, ITALESIterator>> GetEnumerator()
		{
			return dict.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)dict).GetEnumerator();
		}
	}
}
