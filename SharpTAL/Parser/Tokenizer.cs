//
// Tokenizer.cs
//
// Ported to C# from project Chameleon.
// Original source code: https://github.com/malthe/chameleon/blob/master/src/chameleon/tokenizer.py
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpTAL.Parser
{
	public static class Tokenizer
	{
		static Dictionary<string, string> res;
		static Regex re_xml_spe;
		static Regex re_markup_only_spe;

		static void add(string name, string reg)
		{
			foreach (var key in res.Keys)
				reg = reg.Replace("{" + key + "}", res[key]);
			res.Add(name, reg);
		}

		static Tokenizer()
		{
			res = new Dictionary<string, string>();
			add("TextSE", "[^<]+");
			add("UntilHyphen", "[^-]*-");
			add("Until2Hyphens", "{UntilHyphen}(?:[^-]{UntilHyphen})*-");
			add("CommentCE", "{Until2Hyphens}>?");
			add("UntilRSBs", "[^\\]]*](?:[^\\]]+])*]+");
			add("CDATA_CE", "{UntilRSBs}(?:[^\\]>]{UntilRSBs})*>");
			add("S", "[ \\n\\t\\r]+");
			add("Simple", "[^\"'>/]+");
			add("NameStrt", "[A-Za-z_:]|[^\\x00-\\x7F]");
			add("NameChar", "[A-Za-z0-9_:.-]|[^\\x00-\\x7F]");
			add("Name", "(?:{NameStrt})(?:{NameChar})*");
			add("QuoteSE", "\"[^\"]*\"|'[^']*'");
			add("DT_IdentSE", "{S}{Name}(?:{S}(?:{Name}|{QuoteSE}))*");
			add("MarkupDeclCE", "(?:[^\\]\"'><]+|{QuoteSE})*>");
			add("S1", "[\\n\\r\\t ]");
			add("UntilQMs", "[^?]*\\?+");
			add("PI_Tail", "\\?>|{S1}{UntilQMs}(?:[^>?]{UntilQMs})*>");
			add("DT_ItemSE", "<(?:!(?:--{Until2Hyphens}>|[^-]{MarkupDeclCE})|\\?{Name}(?:{PI_Tail}))|%%{Name};|{S}");
			add("DocTypeCE", "{DT_IdentSE}(?:{S})?(?:\\[(?:{DT_ItemSE})*](?:{S})?)?>?");
			add("DeclCE", "--(?:{CommentCE})?|\\[CDATA\\[(?:{CDATA_CE})?|DOCTYPE(?:{DocTypeCE})?");
			add("PI_CE", "{Name}(?:{PI_Tail})?");
			add("EndTagCE", "{Name}(?:{S})?>?");
			add("AttValSE", "\"[^\"]*\"|'[^']*'");
			add("ElemTagCE", "({Name})(?:({S})({Name})(((?:{S})?=(?:{S})?)(?:{AttValSE}|{Simple})|(?!(?:{S})?=)))*(?:{S})?(/?>)?");
			add("MarkupSPE", "<(?:!(?:{DeclCE})?|\\?(?:{PI_CE})?|/(?:{EndTagCE})?|(?:{ElemTagCE})?)");
			add("XML_SPE", "{TextSE}|{MarkupSPE}");
			add("XML_MARKUP_ONLY_SPE", "{MarkupSPE}");
			add("ElemTagSPE", "<|{Name}");

			re_xml_spe = new Regex(res["XML_SPE"]);
			re_markup_only_spe = new Regex(res["XML_MARKUP_ONLY_SPE"]);
		}

		public static IEnumerable<Token> TokenizeXml(string body, string filename = null)
		{
			foreach (Match match in re_xml_spe.Matches(body))
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
