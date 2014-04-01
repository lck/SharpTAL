//
// AbstractTemplateParser.cs
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

using System.Collections.Generic;

namespace SharpTAL.TemplateParser
{
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
		protected abstract void HandleProcessingInstruction(Element e);
		protected abstract void HandleDefault(string data);

		public void ParseTemplate(string templateBody, string templatePath, Dictionary<string, string> defaultNamespaces)
		{
			IEnumerable<Token> tokens = Tokenizer.TokenizeXml(templateBody, templatePath);

			var parser = new ElementParser(tokens, defaultNamespaces);

			foreach (var e in parser.Parse())
				HandleElement(e);
		}

		void HandleElement(Element e)
		{
			if (e.Kind == ElementKind.Element || e.Kind == ElementKind.StartTag)
			{
				// Start tag
				var name = e.StartTagTokens["name"] as Token;
				var suffix = e.StartTagTokens["suffix"] as Token;
				Location loc = name.Location;
				var tag = new Tag
				{
					Name = name.ToString(),
					Suffix = suffix.ToString(),
					SourcePath = name.Filename,
					LineNumber = loc.Line,
					LinePosition = loc.Position,
					Attributes = new List<TagAttribute>()
				};
				var attrs = e.StartTagTokens["attrs"] as List<Dictionary<string, object>>;
				foreach (var attr in attrs)
				{
					var attrName = attr["name"] as Token;
					var attrValue = attr["value"] as Token;
					var attrEq = attr["eq"] as Token;
					var attrQuote = attr["quote"] as Token;
					var a = new TagAttribute
					{
						Name = attrName.ToString(),
						Value = attrValue.ToString(),
						Eq = attrEq.ToString(),
						Quote = attrQuote.ToString(),
						QuoteEntity = Utils.Char2Entity(attrQuote.ToString())
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
					var endName = e.EndTagTokens["name"] as Token;
					var endSuffix = e.EndTagTokens["suffix"] as Token;
					Location endLoc = endName.Location;
					var endTag = new Tag
					{
						Name = endName.ToString(),
						Suffix = endSuffix.ToString(),
						SourcePath = endName.Filename,
						LineNumber = endLoc.Line,
						LinePosition = endLoc.Position
					};
					HandleEndTag(endTag);
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
			else if (e.Kind == ElementKind.ProcessingInstruction)
			{
				HandleProcessingInstruction(e);
			}
			else if (e.Kind == ElementKind.Default)
			{
				foreach (Token token in e.StartTagTokens.Values)
					HandleDefault(token.ToString());
			}
		}
	}
}
