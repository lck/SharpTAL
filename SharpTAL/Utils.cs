//
// Utils.cs
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
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Security.Cryptography;

namespace SharpTAL
{
	class Utils
	{
		public static Assembly ReadAssembly(string asmPath)
		{
			var asmStream = new FileStream(asmPath, FileMode.Open, FileAccess.Read);
			byte[] asmBytes = ReadStream(asmStream);
			Assembly assembly = Assembly.Load(asmBytes);
			return assembly;
		}

		public static byte[] ReadStream(Stream stream)
		{
			var buffer = new byte[32768];
			using (var ms = new MemoryStream())
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
			// Create a byte array from source data
			byte[] tmpSource = Encoding.ASCII.GetBytes(source);

			// Compute hash based on source data
			byte[] tmpHash = new SHA1CryptoServiceProvider().ComputeHash(tmpSource);

			string hash = ByteArrayToString(tmpHash);
			return hash;
		}

		public static string ComputeHash(Dictionary<string, Type> globalsTypes)
		{
			string hash = string.Empty;
			if (globalsTypes != null && globalsTypes.Count > 0)
			{
				var names = new List<string>(globalsTypes.Keys);
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
			var sOutput = new StringBuilder(arrInput.Length);
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
			int cp = c;
			string name;
			if (HtmlEntityDefs.Code2Name.TryGetValue(cp, out name))
				return string.Format("&{0};", name);
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
			string typeName;
			if (names.ContainsKey(type.FullName))
			{
				typeName = names[type.FullName];
			}
			else
			{
				if (type.IsGenericType)
				{
					if (type.ContainsGenericParameters)
					{
						throw new InvalidOperationException("Generic types with generic parameters are not supported in globals dictionary.");
					}
					Type genericType = type.GetGenericTypeDefinition();
					string genericTypeName = string.Format("{0}.{1}",
						genericType.Namespace, genericType.Name.Substring(0, genericType.Name.IndexOf('`')));
					string genericArgs = string.Join(",",
						type.GetGenericArguments().Select(typeArg => GetFullTypeName(typeArg, names)).ToArray());
					typeName = string.Format("{0}<{1}>", genericTypeName, genericArgs);
				}
				else
				{
					typeName = type.FullName.Replace("+", ".");
				}
				names[type.FullName] = typeName;
			}
			return typeName;
		}

		public static Type GetGenericType(Type type)
		{
			Type baseType = type;
			while (baseType != typeof(object))
			{
				if (baseType.IsGenericType)
					return baseType;
				baseType = baseType.BaseType;
			}
			return null;
		}

		public static List<Type> GetGenericTypeArguments(Type type)
		{
			var genericTypeArguments = new List<Type>();
			Type genericType = GetGenericType(type);
			if (genericType != null)
			{
				foreach (var innerType in genericType.GetGenericArguments())
				{
					if (innerType.IsGenericType && !genericTypeArguments.Contains(innerType))
					{
						genericTypeArguments.AddRange(GetGenericTypeArguments(innerType));
					}
					genericTypeArguments.Add(innerType);
				}
			}
			else
			{
				genericTypeArguments.Add(type);
			}
			return genericTypeArguments;
		}
	}
}
