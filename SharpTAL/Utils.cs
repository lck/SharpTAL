//
// Utils.cs
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

using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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

			// Create a byte array from source data
			tmpSource = ASCIIEncoding.ASCII.GetBytes(source);

			// Compute hash based on source data
			tmpHash = new SHA1CryptoServiceProvider().ComputeHash(tmpSource);

			string hash = ByteArrayToString(tmpHash);
			return hash;
		}

		public static string ComputeHash(Dictionary<string, Type> globalsTypes)
		{
			string hash = string.Empty;
			if (globalsTypes != null && globalsTypes.Count > 0)
			{
				List<string> names = new List<string>(globalsTypes.Keys);
				names.Sort();
				foreach (string name in names)
				{
					Type type = globalsTypes[name];
					hash += name + type.FullName;
				}
				hash = ComputeHash(hash);
			}
			return hash;
		}

		public static string ComputeHash(List<Assembly> assemblies)
		{
			string hash = string.Empty;
			if (assemblies != null && assemblies.Count > 0)
			{
				foreach (Assembly asm in assemblies)
				{
					hash += asm.FullName;
				}
				hash = ComputeHash(hash);
			}
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

		public static string Char2Entity(string chr)
		{
			if (string.IsNullOrEmpty(chr))
				return chr;
			char c = chr.Substring(0, 1)[0];
			int cp = (int)c;
			string name = null;
			if (HTMLEntityDefs.Code2Name.TryGetValue(cp, out name))
				return string.Format("&{0};", name);
			else
				return string.Format("&#{0};", cp);
		}

		public static void GetExtensionMethodNamespaces(Assembly assembly, List<string> namespaces)
		{
			if (assembly.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
			{
				foreach (Type tp in assembly.GetTypes())
				{
					// Check if type has defined "ExtensionAttribute"
					if (tp.IsSealed && !tp.IsGenericType && !tp.IsNested &&
						tp.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
					{
						if (!namespaces.Contains(tp.Namespace))
						{
							namespaces.Add(tp.Namespace);
						}
					}
				}
			}
		}

		public static string GetFullTypeName(Type type, Dictionary<string, string> names)
		{
			string typeName = "";
			if (names.ContainsKey(type.FullName))
			{
				typeName = names[type.FullName];
			}
			else
			{
				if (type.IsGenericType)
				{
					typeName = string.Format("{0}.{1}<", type.Namespace, type.Name.Split('`')[0]);
					Type[] typeArguments = type.GetGenericArguments();
					bool first = true;
					foreach (Type typeArg in typeArguments)
					{
						if (!typeArg.IsGenericParameter)
						{
							if (!first)
							{
								typeName = string.Format("{0}, ", typeName);
							}
							first = false;
							string typeArgTypeName = GetFullTypeName(typeArg, names);
							typeName = string.Format("{0}{1}", typeName, typeArgTypeName);
						}
						else
						{
							// TODO: ???
						}
					}
					typeName = string.Format("{0}>", typeName);
				}
				else
				{
					typeName = type.FullName.Replace("+", ".");
				}
				names[type.FullName] = typeName;
			}
			return typeName;
		}
	}
}
