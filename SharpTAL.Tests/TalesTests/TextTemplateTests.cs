using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace SharpTAL.Tests.TalesTests
{
	[TestFixture]
	public class TextTemplateTests
	{
		public static Dictionary<string, object> globals;

		public static void RunTest(string template, string expected, string errMsg)
		{
			globals = new Dictionary<string, object>();
			globals.Add("name", "world");
			string actual = new Template(template).Render(globals);
			actual = actual.Replace("{", "{").Replace("}", "}");
			Assert.AreEqual(expected, actual, "{1} - {0}template: {2}{0}actual: {3}{0}expected: {4}",
				Environment.NewLine, errMsg, template, actual, expected);
		}

		[Test]
		public void TestTextTemplate()
		{
			RunTest(@"Hello ${name}!",
			   @"Hello world!",
			   "Text template failed!");
		}
	}
}
