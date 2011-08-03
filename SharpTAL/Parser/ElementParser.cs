//
// ElementParser.cs
//
// Ported to C# from project Chameleon.
// Original source code: https://github.com/malthe/chameleon/blob/master/src/chameleon/parser.py
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

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpTAL.Parser
{
	public class Element
	{
		public string Kind { get; private set; }
		public Dictionary<string, object> StartTagTokens { get; private set; }
		public Dictionary<string, object> EndTagTokens { get; private set; }
		public List<Element> Children { get; private set; }

		public Element(string kind, Token startTag)
		{
			Kind = kind;
			StartTagTokens = new Dictionary<string, object> { { "", startTag } };
			EndTagTokens = new Dictionary<string, object>();
			Children = new List<Element>();
		}

		public Element(string kind, Dictionary<string, object> startTagTokens)
		{
			Kind = kind;
			StartTagTokens = startTagTokens;
			EndTagTokens = new Dictionary<string, object>();
			Children = new List<Element>();
		}

		public Element(string kind, Dictionary<string, object> startTagTokens, Dictionary<string, object> endTagTokens, List<Element> children)
		{
			Kind = kind;
			StartTagTokens = startTagTokens;
			EndTagTokens = endTagTokens;
			Children = children;
		}
	}

	/// <summary>
	/// Parses tokens into elements.
	/// </summary>
	public class ElementParser
	{
		static readonly Regex _match_tag_prefix_and_name = new Regex(
			@"^(?<prefix></?)(?<name>([^:\n ]+:)?[^ \n\t>/]+)(?<suffix>(?<space>\s*)/?>)?", RegexOptions.Singleline);
		static readonly Regex match_single_attribute = new Regex(
			@"(?<space>\s+)(?!\d)" +
			@"(?<name>[^ =/>\n\t]+)" +
			@"((?<eq>\s*=\s*)" +
			@"((?<quote>[\'""])(?<value>.*?)\k<quote>|" +
			@"(?<alt_value>[^\s\'"">/]+))|" +
			@"(?<simple_value>(?![ \\n\\t\\r]*=)))", RegexOptions.Singleline);
		static readonly Regex _match_comment = new Regex(@"^<!--(?<text>.*)-->$", RegexOptions.Singleline);
		static readonly Regex _match_cdata = new Regex(@"^<!\[CDATA\[(?<text>.*)\]>$", RegexOptions.Singleline);
		static readonly Regex _match_declaration = new Regex(@"^<!(?<text>[^>]+)>$", RegexOptions.Singleline);
		static readonly Regex _match_processing_instruction = new Regex(@"^<\?(?<text>.*?)\?>", RegexOptions.Singleline);
		static readonly Regex _match_xml_declaration = new Regex(@"^<\?xml(?=[ /])", RegexOptions.Singleline);

		IEnumerable<Token> _stream;
		List<Dictionary<string, string>> _namespaces;
		List<Element> _queue;
		Stack<KeyValuePair<Token, int>> _index;

		public ElementParser(IEnumerable<Token> stream, Dictionary<string, string> default_namespaces)
		{
			this._stream = stream;
			this._queue = new List<Element>();
			this._index = new Stack<KeyValuePair<Token, int>>();
			this._namespaces = new List<Dictionary<string, string>> { new Dictionary<string, string>(default_namespaces) };
		}

		public IEnumerable<Element> Parse()
		{
			foreach (var token in this._stream)
			{
				var item = this.ParseToken(token);
				this._queue.Add(item);
			}
			return this._queue;
		}

		Element ParseToken(Token token)
		{
			string kind = IdentifyToken(token);
			if (kind == "comment")
				return visit_comment(kind, token);
			if (kind == "end_tag")
				return visit_end_tag(kind, token);
			if (kind == "empty_tag")
				return visit_empty_tag(kind, token);
			if (kind == "start_tag")
				return visit_start_tag(kind, token);
			if (kind == "text")
				return visit_text(kind, token);
			return visit_default(kind, token);
		}

		static string IdentifyToken(Token token)
		{
			string s = token.ToString();
			if (s.StartsWith("<"))
			{
				if (s.StartsWith("<!--"))
					return "comment";
				if (s.StartsWith("<![CDATA["))
					return "cdata";
				if (s.StartsWith("<!"))
					return "declaration";
				if (s.StartsWith("<?xml"))
					return "xml_declaration";
				if (s.StartsWith("<?"))
					return "processing_instruction";
				if (s.StartsWith("</"))
					return "end_tag";
				if (s.EndsWith("/>"))
					return "empty_tag";
				if (s.EndsWith(">"))
					return "start_tag";
				return "error";
			}
			return "text";
		}

		Element visit_comment(string kind, Token token)
		{
			return new Element("comment", token);
		}

		Element visit_default(string kind, Token token)
		{
			return new Element("default", token);
		}

		Element visit_text(string kind, Token token)
		{
			return new Element(kind, token);
		}

		Element visit_start_tag(string kind, Token token)
		{
			var ns = new Dictionary<string, string>(_namespaces.Last());
			_namespaces.Add(ns);
			var node = parse_tag(token, ns);
			_index.Push(new KeyValuePair<Token, int>(node["name"] as Token, _queue.Count));
			return new Element(kind, node);
		}

		Element visit_end_tag(string kind, Token token)
		{
			Dictionary<string, string> ns;
			try
			{
				ns = _namespaces.Last();
				_namespaces.RemoveAt(_namespaces.Count - 1);
			}
			catch (InvalidOperationException ex)
			{
				throw new ParseError("Unexpected end tag.", token);
			}
			Dictionary<string, object> node = parse_tag(token, ns); ;
			while (_index.Count > 0)
			{
				KeyValuePair<Token, int> idx = _index.Pop();
				Token name = idx.Key;
				int pos = idx.Value;
				if (node["name"].Equals(name))
				{
					Element el = _queue[pos];
					_queue.RemoveAt(pos);
					Dictionary<string, object> start = el.StartTagTokens;
					List<Element> children = _queue.GetRange(pos, _queue.Count - pos);
					_queue.RemoveRange(pos, _queue.Count - pos);
					return new Element("element", start, node, children);
				}
			}
			throw new ParseError("Unexpected end tag.", token);
		}

		Element visit_empty_tag(string kind, Token token)
		{
			var ns = new Dictionary<string, string>(_namespaces.Last());
			var node = parse_tag(token, ns);
			return new Element("element", node);
		}

		static Dictionary<string, object> groupdict(Regex r, Match m, Token token)
		{
			Dictionary<string, object> d = new Dictionary<string, object>();
			foreach (string name in r.GetGroupNames())
			{
				Group g = m.Groups[name];
				if (g != null)
				{
					int i = g.Index;
					int j = g.Length;
					d.Add(name, token.SubString(i, j));
				}
				else
				{
					d.Add(name, null);
				}
			}
			return d;
		}

		static Dictionary<string, object> match_tag(Token token)
		{
			Match match = _match_tag_prefix_and_name.Match(token.ToString());
			Dictionary<string, object> node = groupdict(_match_tag_prefix_and_name, match, token);

			int end = match.Index + match.Length;
			token = token.SubString(end, -1);

			var attrs = new List<Dictionary<string, object>>();
			node["attrs"] = attrs;

			foreach (Match m in match_single_attribute.Matches(token.ToString()))
			{
				Dictionary<string, object> attr = groupdict(match_single_attribute, m, token);
				Token alt_value = null;
				if (attr.Keys.Contains("alt_value"))
				{
					alt_value = attr["alt_value"] as Token;
					attr.Remove("alt_value");
					if (!string.IsNullOrEmpty(alt_value.ToString()))
					{
						attr["value"] = alt_value;
						attr["quote"] = "";
					}
				}
				Token simple_value = null;
				if (attr.Keys.Contains("simple_value"))
				{
					simple_value = attr["simple_value"] as Token;
					attr.Remove("simple_value");
					if (!string.IsNullOrEmpty(simple_value.ToString()))
					{
						attr["quote"] = "";
						attr["value"] = new Token("");
						attr["eq"] = "";
					}
				}
				attrs.Add(attr);
				int m_end = m.Index + m.Length;
				node["suffix"] = token.SubString(m_end, -1);
			}

			return node;
		}

		static Dictionary<string, object> parse_tag(Token token, Dictionary<string, string> ns)
		{
			var node = match_tag(token);

			update_namespace(node["attrs"] as List<Dictionary<string, object>>, ns);

			string prefix = null;
			if ((node["name"] as Token).ToString().Contains(':'))
				prefix = (node["name"] as Token).ToString().Split(':')[0];

			string defaultNs = prefix != null && ns.ContainsKey(prefix) ? ns[prefix] : Namespaces.XML_NS;
			node["namespace"] = defaultNs;
			node["ns_attrs"] = unpack_attributes(node["attrs"] as List<Dictionary<string, object>>, ns, defaultNs);

			return node;
		}

		static void update_namespace(List<Dictionary<string, object>> attributes, Dictionary<string, string> ns)
		{
			foreach (var attribute in attributes)
			{
				string name = ((Token)attribute["name"]).ToString();
				string value = ((Token)attribute["value"]).ToString();

				if (name == "xmlns")
					ns[""] = value;
				else if (name.ToString().StartsWith("xmlns:"))
					ns[name.Substring(6)] = value;
			}
		}

		static OrderedDictionary unpack_attributes(List<Dictionary<string, object>> attributes, Dictionary<string, string> ns, string defaultNs)
		{
			OrderedDictionary namespaced = new OrderedDictionary();

			foreach (var attribute in attributes)
			{
				string name = ((Token)attribute["name"]).ToString();
				string value = ((Token)attribute["value"]).ToString();

				string n = null;
				string prefix = null;
				if (name.Contains(':'))
				{
					prefix = name.Split(':')[0];
					name = name.Substring(prefix.Length + 1);
					try
					{
						n = ns[prefix];
					}
					catch (IndexOutOfRangeException ex)
					{
						throw new IndexOutOfRangeException(
							string.Format("Undefined namespace prefix: {0}.", prefix));
					}
				}
				else
					n = defaultNs;

				namespaced[new KeyValuePair<string, string>(n, name)] = value;
			}

			return namespaced;
		}
	}
}
