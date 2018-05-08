using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace SharpTAL.Tests.TalTests
{
	[TestFixture]
	public class TalOmitTagTests
	{
		public static Dictionary<string, object> globals;

		[OneTimeSetUp]
		public void SetUpClass()
		{
		}

		[OneTimeTearDown]
		public void CleanupClass()
		{
		}

		[SetUp]
		public void SetUp()
		{
			globals = new Dictionary<string, object>();
			globals.Add("test", "testing"); ;
			globals.Add("link", "www.owlfish.com"); ;
		}

		public static void RunTest(string template, string expected, string errMsg)
		{
			string actual = new Template(template).Render(globals);
			actual = actual.Replace("{", "{{").Replace("}", "}}");
			Assert.AreEqual(expected, actual, "{1} - {0}template: {2}{0}actual: {3}{0}expected: {4}",
				Environment.NewLine, errMsg, template, actual, expected);
		}

		[Test]
		public void TestOmitTagTrue()
		{
			RunTest(@"<html tal:omit-tag=""link"" href=""owlfish.com"">Hello</html>"
				, "Hello"
				, "Omit tag, true, failed.");
		}

		[Test]
		public void TestOmitTagEmptyArg()
		{
			RunTest(@"<html tal:omit-tag="""" href=""owlfish.com"">Hello</html>"
				, "Hello"
				, "Omit tag, empty arg, failed.");
		}
	}
}
