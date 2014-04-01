//
// Tokenizer.cs
//
// Ported to C# from project Chameleon.
// Original source code: https://github.com/malthe/chameleon/blob/master/src/chameleon/tokenizer.py
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
using System.Text.RegularExpressions;

namespace SharpTAL.TemplateParser
{
	public static class Tokenizer
	{
		private static readonly Dictionary<string, string> Res;
		private static readonly Regex ReXmlSpe;

		private static void Add(string name, string reg)
		{
			foreach (var key in Res.Keys)
				reg = reg.Replace("{" + key + "}", Res[key]);
			Res.Add(name, reg);
		}

		static Tokenizer()
		{
			Res = new Dictionary<string, string>();
			Add("TextSE", "[^<]+");
			Add("UntilHyphen", "[^-]*-");
			Add("Until2Hyphens", "{UntilHyphen}(?:[^-]{UntilHyphen})*-");
			Add("CommentCE", "{Until2Hyphens}>?");
			Add("UntilRSBs", "[^\\]]*](?:[^\\]]+])*]+");
			Add("CDATA_CE", "{UntilRSBs}(?:[^\\]>]{UntilRSBs})*>");
			Add("S", "[ \\n\\t\\r]+");
			Add("Simple", "[^\"'>/]+");
			Add("NameStrt", "[A-Za-z_:]|[^\\x00-\\x7F]");
			Add("NameChar", "[A-Za-z0-9_:.-]|[^\\x00-\\x7F]");
			Add("Name", "(?:{NameStrt})(?:{NameChar})*");
			Add("QuoteSE", "\"[^\"]*\"|'[^']*'");
			Add("DT_IdentSE", "{S}{Name}(?:{S}(?:{Name}|{QuoteSE}))*");
			Add("MarkupDeclCE", "(?:[^\\]\"'><]+|{QuoteSE})*>");
			Add("S1", "[\\n\\r\\t ]");
			Add("UntilQMs", "[^?]*\\?+");
			Add("PI_Tail", "\\?>|{S1}{UntilQMs}(?:[^>?]{UntilQMs})*>");
			Add("DT_ItemSE", "<(?:!(?:--{Until2Hyphens}>|[^-]{MarkupDeclCE})|\\?{Name}(?:{PI_Tail}))|%%{Name};|{S}");
			Add("DocTypeCE", "{DT_IdentSE}(?:{S})?(?:\\[(?:{DT_ItemSE})*](?:{S})?)?>?");
			Add("DeclCE", "--(?:{CommentCE})?|\\[CDATA\\[(?:{CDATA_CE})?|DOCTYPE(?:{DocTypeCE})?");
			Add("PI_CE", "{Name}(?:{PI_Tail})?");
			Add("EndTagCE", "{Name}(?:{S})?>?");
			Add("AttValSE", "\"[^\"]*\"|'[^']*'");
			Add("ElemTagCE", "({Name})(?:({S})({Name})(((?:{S})?=(?:{S})?)(?:{AttValSE}|{Simple})|(?!(?:{S})?=)))*(?:{S})?(/?>)?");
			Add("MarkupSPE", "<(?:!(?:{DeclCE})?|\\?(?:{PI_CE})?|/(?:{EndTagCE})?|(?:{ElemTagCE})?)");
			Add("XML_SPE", "{TextSE}|{MarkupSPE}");
			Add("XML_MARKUP_ONLY_SPE", "{MarkupSPE}");
			Add("ElemTagSPE", "<|{Name}");

			ReXmlSpe = new Regex(Res["XML_SPE"]);
		}

		public static IEnumerable<Token> TokenizeXml(string body, string filename = null)
		{
			foreach (Match match in ReXmlSpe.Matches(body))
			{
				string token = match.Value;
				int position = match.Index;
				yield return new Token(token, position, body, filename);
			}
		}

		public static IEnumerable<Token> TokenizeText(string body, string filename = null)
		{
			yield return new Token(body, 0, body, filename);
		}
	}
}
