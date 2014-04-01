//
// RepeatDictionary.cs
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

using System.Collections;
using System.Collections.Generic;

namespace SharpTAL
{
	/// <summary>
	/// Repeat dictionary implementation
	/// </summary>
	public class RepeatDictionary : IRepeatDictionary
	{
		private readonly IDictionary<string, ITalesIterator> _dict;

		public RepeatDictionary()
		{
			_dict = new Dictionary<string, ITalesIterator>();
		}

		public void Add(string key, ITalesIterator value)
		{
			_dict.Add(key, value);
		}

		public bool ContainsKey(string key)
		{
			return _dict.ContainsKey(key);
		}

		public ICollection<string> Keys
		{
			get { return _dict.Keys; }
		}

		public bool Remove(string key)
		{
			return _dict.Remove(key);
		}

		public bool TryGetValue(string key, out ITalesIterator value)
		{
			return _dict.TryGetValue(key, out value);
		}

		public ICollection<ITalesIterator> Values
		{
			get { return _dict.Values; }
		}

		public ITalesIterator this[string key]
		{
			get
			{
				return _dict[key];
			}
			set
			{
				_dict[key] = value;
			}
		}

		public void Add(KeyValuePair<string, ITalesIterator> item)
		{
			_dict.Add(item);
		}

		public void Clear()
		{
			_dict.Clear();
		}

		public bool Contains(KeyValuePair<string, ITalesIterator> item)
		{
			return _dict.Contains(item);
		}

		public void CopyTo(KeyValuePair<string, ITalesIterator>[] array, int arrayIndex)
		{
			_dict.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return _dict.Count; }
		}

		public bool IsReadOnly
		{
			get { return _dict.IsReadOnly; }
		}

		public bool Remove(KeyValuePair<string, ITalesIterator> item)
		{
			return _dict.Remove(item);
		}

		public IEnumerator<KeyValuePair<string, ITalesIterator>> GetEnumerator()
		{
			return _dict.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_dict).GetEnumerator();
		}
	}
}
