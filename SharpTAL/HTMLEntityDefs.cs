//
// SGMLEntityNames.cs
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

using System.Collections.Generic;

namespace SharpTAL
{
	internal sealed class HTMLEntityDefs
	{
		public static readonly Dictionary<string, int> Name2Code = new Dictionary<string, int>()
        {
            { "&zwnj;", 8204 }
            ,{ "&aring;", 229 }
            ,{ "&gt;", 62 }
            ,{ "&yen;", 165 }
            ,{ "&ograve;", 242 }
            ,{ "&Chi;", 935 }
            ,{ "&delta;", 948 }
            ,{ "&rang;", 9002 }
            ,{ "&sup;", 8835 }
            ,{ "&trade;", 8482 }
            ,{ "&Ntilde;", 209 }
            ,{ "&xi;", 958 }
            ,{ "&upsih;", 978 }
            ,{ "&nbsp;", 160 }
            ,{ "&Atilde;", 195 }
            ,{ "&radic;", 8730 }
            ,{ "&otimes;", 8855 }
            ,{ "&aelig;", 230 }
            ,{ "&oelig;", 339 }
            ,{ "&equiv;", 8801 }
            ,{ "&ni;", 8715 }
            ,{ "&infin;", 8734 }
            ,{ "&Psi;", 936 }
            ,{ "&auml;", 228 }
            ,{ "&cup;", 8746 }
            ,{ "&Epsilon;", 917 }
            ,{ "&otilde;", 245 }
            ,{ "&lt;", 60 }
            ,{ "&Icirc;", 206 }
            ,{ "&Eacute;", 201 }
            ,{ "&Lambda;", 923 }
            ,{ "&sbquo;", 8218 }
            ,{ "&Prime;", 8243 }
            ,{ "&prime;", 8242 }
            ,{ "&psi;", 968 }
            ,{ "&Kappa;", 922 }
            ,{ "&rsaquo;", 8250 }
            ,{ "&Tau;", 932 }
            ,{ "&uacute;", 250 }
            ,{ "&ocirc;", 244 }
            ,{ "&lrm;", 8206 }
            ,{ "&zwj;", 8205 }
            ,{ "&cedil;", 184 }
            ,{ "&Alpha;", 913 }
            ,{ "&not;", 172 }
            ,{ "&amp;", 38 }
            ,{ "&AElig;", 198 }
            ,{ "&oslash;", 248 }
            ,{ "&acute;", 180 }
            ,{ "&lceil;", 8968 }
            ,{ "&alefsym;", 8501 }
            ,{ "&laquo;", 171 }
            ,{ "&shy;", 173 }
            ,{ "&loz;", 9674 }
            ,{ "&ge;", 8805 }
            ,{ "&Igrave;", 204 }
            ,{ "&nu;", 957 }
            ,{ "&Ograve;", 210 }
            ,{ "&lsaquo;", 8249 }
            ,{ "&sube;", 8838 }
            ,{ "&euro;", 8364 }
            ,{ "&rarr;", 8594 }
            ,{ "&sdot;", 8901 }
            ,{ "&rdquo;", 8221 }
            ,{ "&Yacute;", 221 }
            ,{ "&lfloor;", 8970 }
            ,{ "&lArr;", 8656 }
            ,{ "&Auml;", 196 }
            ,{ "&Dagger;", 8225 }
            ,{ "&brvbar;", 166 }
            ,{ "&Otilde;", 213 }
            ,{ "&szlig;", 223 }
            ,{ "&clubs;", 9827 }
            ,{ "&diams;", 9830 }
            ,{ "&agrave;", 224 }
            ,{ "&Ocirc;", 212 }
            ,{ "&Iota;", 921 }
            ,{ "&Theta;", 920 }
            ,{ "&Pi;", 928 }
            ,{ "&zeta;", 950 }
            ,{ "&Scaron;", 352 }
            ,{ "&frac14;", 188 }
            ,{ "&egrave;", 232 }
            ,{ "&sub;", 8834 }
            ,{ "&iexcl;", 161 }
            ,{ "&frac12;", 189 }
            ,{ "&ordf;", 170 }
            ,{ "&sum;", 8721 }
            ,{ "&prop;", 8733 }
            ,{ "&Uuml;", 220 }
            ,{ "&ntilde;", 241 }
            ,{ "&atilde;", 227 }
            ,{ "&asymp;", 8776 }
            ,{ "&uml;", 168 }
            ,{ "&prod;", 8719 }
            ,{ "&nsub;", 8836 }
            ,{ "&reg;", 174 }
            ,{ "&rArr;", 8658 }
            ,{ "&Oslash;", 216 }
            ,{ "&emsp;", 8195 }
            ,{ "&THORN;", 222 }
            ,{ "&yuml;", 255 }
            ,{ "&aacute;", 225 }
            ,{ "&Mu;", 924 }
            ,{ "&hArr;", 8660 }
            ,{ "&le;", 8804 }
            ,{ "&thinsp;", 8201 }
            ,{ "&dArr;", 8659 }
            ,{ "&ecirc;", 234 }
            ,{ "&bdquo;", 8222 }
            ,{ "&Sigma;", 931 }
            ,{ "&Aring;", 197 }
            ,{ "&tilde;", 732 }
            ,{ "&nabla;", 8711 }
            ,{ "&mdash;", 8212 }
            ,{ "&uarr;", 8593 }
            ,{ "&times;", 215 }
            ,{ "&Ugrave;", 217 }
            ,{ "&Eta;", 919 }
            ,{ "&Agrave;", 192 }
            ,{ "&chi;", 967 }
            ,{ "&real;", 8476 }
            ,{ "&circ;", 710 }
            ,{ "&eth;", 240 }
            ,{ "&rceil;", 8969 }
            ,{ "&iuml;", 239 }
            ,{ "&gamma;", 947 }
            ,{ "&lambda;", 955 }
            ,{ "&harr;", 8596 }
            ,{ "&Egrave;", 200 }
            ,{ "&frac34;", 190 }
            ,{ "&dagger;", 8224 }
            ,{ "&divide;", 247 }
            ,{ "&Ouml;", 214 }
            ,{ "&image;", 8465 }
            ,{ "&ndash;", 8211 }
            ,{ "&hellip;", 8230 }
            ,{ "&igrave;", 236 }
            ,{ "&Yuml;", 376 }
            ,{ "&ang;", 8736 }
            ,{ "&alpha;", 945 }
            ,{ "&frasl;", 8260 }
            ,{ "&ETH;", 208 }
            ,{ "&lowast;", 8727 }
            ,{ "&Nu;", 925 }
            ,{ "&plusmn;", 177 }
            ,{ "&bull;", 8226 }
            ,{ "&sup1;", 185 }
            ,{ "&sup2;", 178 }
            ,{ "&sup3;", 179 }
            ,{ "&Aacute;", 193 }
            ,{ "&cent;", 162 }
            ,{ "&oline;", 8254 }
            ,{ "&Beta;", 914 }
            ,{ "&perp;", 8869 }
            ,{ "&Delta;", 916 }
            ,{ "&there4;", 8756 }
            ,{ "&pi;", 960 }
            ,{ "&iota;", 953 }
            ,{ "&empty;", 8709 }
            ,{ "&euml;", 235 }
            ,{ "&notin;", 8713 }
            ,{ "&iacute;", 237 }
            ,{ "&para;", 182 }
            ,{ "&epsilon;", 949 }
            ,{ "&weierp;", 8472 }
            ,{ "&OElig;", 338 }
            ,{ "&uuml;", 252 }
            ,{ "&larr;", 8592 }
            ,{ "&icirc;", 238 }
            ,{ "&Upsilon;", 933 }
            ,{ "&omicron;", 959 }
            ,{ "&upsilon;", 965 }
            ,{ "&copy;", 169 }
            ,{ "&Iuml;", 207 }
            ,{ "&Oacute;", 211 }
            ,{ "&Xi;", 926 }
            ,{ "&kappa;", 954 }
            ,{ "&ccedil;", 231 }
            ,{ "&Ucirc;", 219 }
            ,{ "&cap;", 8745 }
            ,{ "&mu;", 956 }
            ,{ "&scaron;", 353 }
            ,{ "&lsquo;", 8216 }
            ,{ "&isin;", 8712 }
            ,{ "&Zeta;", 918 }
            ,{ "&minus;", 8722 }
            ,{ "&deg;", 176 }
            ,{ "&and;", 8743 }
            ,{ "&tau;", 964 }
            ,{ "&pound;", 163 }
            ,{ "&curren;", 164 }
            ,{ "&int;", 8747 }
            ,{ "&ucirc;", 251 }
            ,{ "&rfloor;", 8971 }
            ,{ "&ensp;", 8194 }
            ,{ "&crarr;", 8629 }
            ,{ "&ugrave;", 249 }
            ,{ "&exist;", 8707 }
            ,{ "&cong;", 8773 }
            ,{ "&theta;", 952 }
            ,{ "&oplus;", 8853 }
            ,{ "&permil;", 8240 }
            ,{ "&Acirc;", 194 }
            ,{ "&piv;", 982 }
            ,{ "&Euml;", 203 }
            ,{ "&Phi;", 934 }
            ,{ "&Iacute;", 205 }
            ,{ "&quot;", 34 }
            ,{ "&Uacute;", 218 }
            ,{ "&Omicron;", 927 }
            ,{ "&ne;", 8800 }
            ,{ "&iquest;", 191 }
            ,{ "&eta;", 951 }
            ,{ "&rsquo;", 8217 }
            ,{ "&yacute;", 253 }
            ,{ "&Rho;", 929 }
            ,{ "&darr;", 8595 }
            ,{ "&Ecirc;", 202 }
            ,{ "&Omega;", 937 }
            ,{ "&acirc;", 226 }
            ,{ "&sim;", 8764 }
            ,{ "&phi;", 966 }
            ,{ "&sigmaf;", 962 }
            ,{ "&macr;", 175 }
            ,{ "&thetasym;", 977 }
            ,{ "&Ccedil;", 199 }
            ,{ "&ordm;", 186 }
            ,{ "&uArr;", 8657 }
            ,{ "&forall;", 8704 }
            ,{ "&beta;", 946 }
            ,{ "&fnof;", 402 }
            ,{ "&rho;", 961 }
            ,{ "&micro;", 181 }
            ,{ "&eacute;", 233 }
            ,{ "&omega;", 969 }
            ,{ "&middot;", 183 }
            ,{ "&Gamma;", 915 }
            ,{ "&rlm;", 8207 }
            ,{ "&lang;", 9001 }
            ,{ "&spades;", 9824 }
            ,{ "&supe;", 8839 }
            ,{ "&thorn;", 254 }
            ,{ "&ouml;", 246 }
            ,{ "&or;", 8744 }
            ,{ "&raquo;", 187 }
            ,{ "&part;", 8706 }
            ,{ "&sect;", 167 }
            ,{ "&ldquo;", 8220 }
            ,{ "&hearts;", 9829 }
            ,{ "&sigma;", 963 }
            ,{ "&oacute;", 243 }
        };
	}
}
