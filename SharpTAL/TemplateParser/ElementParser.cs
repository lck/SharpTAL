//
// ElementParser.cs
//
// Ported to C# from project Chameleon.
// Original source code: https://github.com/malthe/chameleon/blob/master/src/chameleon/parser.py
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
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;

namespace SharpTAL.TemplateParser
{
	/// <summary>
	/// Parses tokens into elements.
	/// </summary>
	public class ElementParser
	{
		private const string XmlNs = "http://www.w3.org/XML/1998/namespace";

		private static readonly Regex MatchTagPrefixAndName = new Regex(
			@"^(?<prefix></?)(?<name>([^:\n ]+:)?[^ \r\n\t>/]+)(?<suffix>(?<space>\s*)/?>)?", RegexOptions.Singleline);
		private static readonly Regex MatchSingleAttribute = new Regex(
			@"(?<space>\s+)(?!\d)" +
			@"(?<name>[^ =/>\n\t]+)" +
			@"((?<eq>\s*=\s*)" +
			@"((?<quote>[\'""])(?<value>.*?)\k<quote>|" +
			@"(?<alt_value>[^\s\'"">/]+))|" +
			@"(?<simple_value>(?![ \\n\\t\\r]*=)))", RegexOptions.Singleline);
		//private static readonly Regex MatchComment = new Regex(
		//	@"^<!--(?<text>.*)-->$", RegexOptions.Singleline);
		//private static readonly Regex MatchCdata = new Regex(
		//	@"^<!\[CDATA\[(?<text>.*)\]>$", RegexOptions.Singleline);
		//private static readonly Regex MatchDeclaration = new Regex(
		//	@"^<!(?<text>[^>]+)>$", RegexOptions.Singleline);
		private static readonly Regex MatchProcessingInstruction = new Regex(
			@"^<\?(?<name>\w+)(?<text>.*?)\?>", RegexOptions.Singleline);
		//private static readonly Regex MatchXmlDeclaration = new Regex(
		//	@"^<\?xml(?=[ /])", RegexOptions.Singleline);

		private readonly IEnumerable<Token> _stream;
		private readonly List<Dictionary<string, string>> _namespaces;
		private readonly List<Element> _queue;
		private readonly Stack<KeyValuePair<Token, int>> _index;

		public ElementParser(IEnumerable<Token> stream, Dictionary<string, string> defaultNamespaces)
		{
			_stream = stream;
			_queue = new List<Element>();
			_index = new Stack<KeyValuePair<Token, int>>();
			_namespaces = new List<Dictionary<string, string>> { new Dictionary<string, string>(defaultNamespaces) };
		}

		public IEnumerable<Element> Parse()
		{
			foreach (var token in _stream)
			{
				var item = ParseToken(token);
				_queue.Add(item);
			}
			return _queue;
		}

		Element ParseToken(Token token)
		{
			TokenKind kind = token.Kind;
			if (kind == TokenKind.Comment)
				return visit_comment(token);
			if (kind == TokenKind.CData)
				return visit_cdata(token);
			if (kind == TokenKind.ProcessingInstruction)
				return visit_processing_instruction(token);
			if (kind == TokenKind.EndTag)
				return visit_end_tag(token);
			if (kind == TokenKind.EmptyTag)
				return visit_empty_tag(token);
			if (kind == TokenKind.StartTag)
				return visit_start_tag(token);
			if (kind == TokenKind.Text)
				return visit_text(token);
			return visit_default(token);
		}

		Element visit_comment(Token token)
		{
			return new Element(ElementKind.Comment, token);
		}

		Element visit_default(Token token)
		{
			return new Element(ElementKind.Default, token);
		}

		Element visit_text(Token token)
		{
			return new Element(ElementKind.Text, token);
		}

		Element visit_cdata(Token token)
		{
			return new Element(ElementKind.CData, token);
		}

		Element visit_processing_instruction(Token token)
		{
			Match match = MatchProcessingInstruction.Match(token.ToString());
			Dictionary<string, object> node = Groupdict(MatchProcessingInstruction, match, token);
			return new Element(ElementKind.ProcessingInstruction, node);
		}

		Element visit_start_tag(Token token)
		{
			var ns = new Dictionary<string, string>(_namespaces.Last());
			_namespaces.Add(ns);
			var node = parse_tag(token, ns);
			_index.Push(new KeyValuePair<Token, int>(node["name"] as Token, _queue.Count));
			return new Element(ElementKind.StartTag, node);
		}

		Element visit_end_tag(Token token)
		{
			Dictionary<string, string> ns;
			try
			{
				ns = _namespaces.Last();
				_namespaces.RemoveAt(_namespaces.Count - 1);
			}
			catch (InvalidOperationException)
			{
				throw new ParseError("Unexpected end tag.", token);
			}
			Dictionary<string, object> node = parse_tag(token, ns);
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
					return new Element(ElementKind.Element, start, node, children);
				}
			}
			throw new ParseError("Unexpected end tag.", token);
		}

		Element visit_empty_tag(Token token)
		{
			var ns = new Dictionary<string, string>(_namespaces.Last());
			var node = parse_tag(token, ns);
			return new Element(ElementKind.Element, node);
		}

		static Dictionary<string, object> Groupdict(Regex r, Match m, Token token)
		{
			var d = new Dictionary<string, object>();
			foreach (string name in r.GetGroupNames())
			{
				Group g = m.Groups[name];
				if (g != null)
				{
					int i = g.Index;
					int j = g.Length;
					d.Add(name, token.Substring(i, j));
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
			Match match = MatchTagPrefixAndName.Match(token.ToString());
			Dictionary<string, object> node = Groupdict(MatchTagPrefixAndName, match, token);

			int end = match.Index + match.Length;
			token = token.Substring(end);

			var attrs = new List<Dictionary<string, object>>();
			node["attrs"] = attrs;

			foreach (Match m in MatchSingleAttribute.Matches(token.ToString()))
			{
				Dictionary<string, object> attr = Groupdict(MatchSingleAttribute, m, token);
				if (attr.Keys.Contains("alt_value"))
				{
					var altValue = attr["alt_value"] as Token;
					attr.Remove("alt_value");
					if (!string.IsNullOrEmpty(altValue.ToString()))
					{
						attr["value"] = altValue;
						attr["quote"] = "";
					}
				}
				if (attr.Keys.Contains("simple_value"))
				{
					var simpleValue = attr["simple_value"] as Token;
					attr.Remove("simple_value");
					if (!string.IsNullOrEmpty(simpleValue.ToString()))
					{
						attr["quote"] = "";
						attr["value"] = new Token("");
						attr["eq"] = "";
					}
				}
				attrs.Add(attr);
				int m_end = m.Index + m.Length;
				node["suffix"] = token.Substring(m_end);
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

			string defaultNs = prefix != null && ns.ContainsKey(prefix) ? ns[prefix] : XmlNs;
			node["namespace"] = defaultNs;
			node["ns_attrs"] = unpack_attributes(node["attrs"] as List<Dictionary<string, object>>, ns, defaultNs);

			return node;
		}

		static void update_namespace(IEnumerable<Dictionary<string, object>> attributes, Dictionary<string, string> ns)
		{
			foreach (var attribute in attributes)
			{
				string name = (attribute["name"]).ToString();
				string value = (attribute["value"]).ToString();

				if (name == "xmlns")
					ns[""] = value;
				else if (name.StartsWith("xmlns:"))
					ns[name.Substring(6)] = value;
			}
		}

		static OrderedDictionary unpack_attributes(IEnumerable<Dictionary<string, object>> attributes, IDictionary<string, string> ns, string defaultNs)
		{
			var namespaced = new OrderedDictionary();

			foreach (var attribute in attributes)
			{
				string name = (attribute["name"]).ToString();
				string value = (attribute["value"]).ToString();

				string n;
				if (name.Contains(':'))
				{
					string prefix = name.Split(':')[0];
					name = name.Substring(prefix.Length + 1);
					try
					{
						n = ns[prefix];
					}
					catch (KeyNotFoundException)
					{
						throw new KeyNotFoundException(
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
