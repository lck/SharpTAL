using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.CodeDom.Compiler;
using System.Security.Cryptography;

using Microsoft.CSharp;

using SharpTAL.Interfaces;

namespace SharpTAL
{
	class Utils
	{
		public static Assembly ReadAssembly(string asmPath)
		{
			FileStream asmStream = new FileStream(asmPath, FileMode.Open, FileAccess.Read);
			byte[] asmBytes = Utils.ReadStream(asmStream);
			Assembly assembly = Assembly.Load(asmBytes);
			return assembly;
		}

		public static byte[] ReadStream(Stream stream)
		{
			byte[] buffer = new byte[32768];
			using (MemoryStream ms = new MemoryStream())
			{
				while (true)
				{
					int read = stream.Read(buffer, 0, buffer.Length);
					if (read <= 0)
						return ms.ToArray();
					ms.Write(buffer, 0, read);
				}
			}
		}

		public static string ComputeHash(string source)
		{
			byte[] tmpSource;
			byte[] tmpHash;

			//Create a byte array from source data
			tmpSource = ASCIIEncoding.ASCII.GetBytes(source);

			//Compute hash based on source data
			tmpHash = new SHA1CryptoServiceProvider().ComputeHash(tmpSource);

			string hash = ByteArrayToString(tmpHash);
			return hash;
		}

		public static string ByteArrayToString(byte[] arrInput)
		{
			int i;
			StringBuilder sOutput = new StringBuilder(arrInput.Length);
			for (i = 0; i < arrInput.Length - 1; i++)
			{
				sOutput.Append(arrInput[i].ToString("X2"));
			}
			return sOutput.ToString();
		}

		public static string EscapeXml(string s)
		{
			return EscapeXml(s, false);
		}

		public static string EscapeXml(string s, bool quote)
		{
			// Replace special characters "&", "<" and ">" to HTML-safe sequences.
			// If the optional flag quote is true, the quotation mark character (")
			// is also translated.'''
			string xml = s;
			if (!string.IsNullOrEmpty(xml))
			{
				xml = xml.Replace("&", "&amp;");
				xml = xml.Replace("<", "&lt;");
				xml = xml.Replace(">", "&gt;");
				if (quote)
					xml = xml.Replace("\"", "&quot;");
			}
			return xml;
		}
	}
}
