//
// Utils.cs
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

using System.IO;
using System.Reflection;
using System.Text;
using System.Security.Cryptography;

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
