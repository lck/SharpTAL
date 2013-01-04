//
// AbstractTemplateParser.cs
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

namespace SharpTAL.TemplateParser
{
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using System.Collections.Specialized;

	/// <summary>
	/// Abstract XML/HTML parser
	/// </summary>
	public abstract class AbstractTemplateParser
	{
		protected abstract void HandleStartTag(Tag tag);
		protected abstract void HandleEndTag(Tag tag);
		protected abstract void HandleData(string data);
		protected abstract void HandleComment(string data);
		protected abstract void HandleCData(string data);
		protected abstract void HandleDefault(string data);

		public void ParseTemplate(string templateBody, string templatePath, Dictionary<string, string> defaultNamespaces)
		{
			IEnumerable<Token> tokens = Tokenizer.TokenizeXml(templateBody, templatePath);

			ElementParser parser = new ElementParser(tokens, defaultNamespaces);

			foreach (var e in parser.Parse())
				HandleElement(e);
		}

		void HandleElement(Element e)
		{
			if (e.Kind == ElementKind.Element || e.Kind == ElementKind.StartTag)
			{
				// Start tag
				Token name = e.StartTagTokens["name"] as Token;
				Token suffix = e.StartTagTokens["suffix"] as Token;
				Tag tag = new Tag();
				tag.Name = name.ToString();
				tag.Suffix = suffix.ToString();
				tag.SourcePath = name.Filename;
				Location loc = name.Location;
				tag.LineNumber = loc.Line;
				tag.LinePosition = loc.Position;
				tag.Attributes = new List<TagAttribute>();
				List<Dictionary<string, object>> attrs = e.StartTagTokens["attrs"] as List<Dictionary<string, object>>;
				foreach (var attr in attrs)
				{
					Token attr_name = attr["name"] as Token;
					Token attr_value = attr["value"] as Token;
					Token attr_eq = attr["eq"] as Token;
					Token attr_quote = attr["quote"] as Token;
					TagAttribute a = new TagAttribute
					{
						Name = attr_name.ToString(),
						Value = attr_value.ToString(),
						Eq = attr_eq.ToString(),
						Quote = attr_quote.ToString(),
						QuoteEntity = Utils.Char2Entity(attr_quote.ToString())
					};
					tag.Attributes.Add(a);
				}
				if ((e.Children.Count == 0 && suffix.ToString() == "/>") || e.EndTagTokens.Count == 0)
				{
					// Singleton element
					tag.Singleton = true;
					HandleStartTag(tag);
					HandleEndTag(tag);
				}
				else
				{
					tag.Singleton = false;
					HandleStartTag(tag);
				}

				// Children
				foreach (var item in e.Children)
					HandleElement(item);

				// End tag
				if (e.EndTagTokens.Count > 0)
				{
					Token end_name = e.EndTagTokens["name"] as Token;
					Token end_suffix = e.EndTagTokens["suffix"] as Token;
					Tag end_tag = new Tag();
					end_tag.Name = end_name.ToString();
					end_tag.Suffix = end_suffix.ToString();
					end_tag.SourcePath = end_name.Filename;
					Location end_loc = end_name.Location;
					end_tag.LineNumber = end_loc.Line;
					end_tag.LinePosition = end_loc.Position;
					HandleEndTag(end_tag);
				}
			}
			else if (e.Kind == ElementKind.Text)
			{
				foreach (Token token in e.StartTagTokens.Values)
					HandleData(token.ToString());
			}
			else if (e.Kind == ElementKind.Comment)
			{
				foreach (Token token in e.StartTagTokens.Values)
					HandleComment(token.ToString());
			}
			else if (e.Kind == ElementKind.CData)
			{
				foreach (Token token in e.StartTagTokens.Values)
					HandleCData(token.ToString());
			}
			else if (e.Kind == ElementKind.Default)
			{
				foreach (Token token in e.StartTagTokens.Values)
					HandleDefault(token.ToString());
			}
		}
	}
}
