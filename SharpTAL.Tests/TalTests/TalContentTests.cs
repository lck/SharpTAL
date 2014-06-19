using System;
using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;

namespace SharpTAL.Tests.TalTests
{
	public static class Extensions
	{
		public static string ToUpperExtension(this string s)
		{
			return s.ToUpper();
		}
	}

	[TestFixture]
	public class TalContentTests
	{
		public static Dictionary<string, object> globals;

		[TestFixtureSetUp]
		public void SetUpClass()
		{
		}

		[TestFixtureTearDown]
		public void CleanupClass()
		{
		}

		[SetUp]
		public void SetUp()
		{
			string entry = @"Some structure: <b tal:content=""weblog/subject""></b>";
			Dictionary<string, string> weblog = new Dictionary<string, string>
            {
                { "subject", "Test subject" },
                { "entry",  entry }
            };
			globals = new Dictionary<string, object>();
			globals.Add("test", "testing");
			globals.Add("one", new List<object>() { 1 });
			globals.Add("two", new List<object>() { "one", "two" });
			globals.Add("three", new List<object>() { 1, "Two", 3 });
			globals.Add("weblog", weblog);
		}

		public static void RunTest(string template, string expected, string errMsg)
		{
			List<Assembly> referencedAssemblies = new List<Assembly>()
			{
				typeof(TalContentTests).Assembly
			};
			string actual = new Template(template, referencedAssemblies).Render(globals);
			actual = actual.Replace("{", "{{").Replace("}", "}}");
			Assert.AreEqual(expected, actual, "{1} - {0}template: {2}{0}actual: {3}{0}expected: {4}",
				Environment.NewLine, errMsg, template, actual, expected);
		}

		[Test]
		public void TestContentExtensionMethods()
		{
			RunTest(@"<html><p tal:content=""test.ToUpperExtension()"">Original</p></html>"
				, "<html><p>TESTING</p></html>"
				, "Content of string did not evaluate to contain string");
		}

		[Test]
		public void TestContentNull()
		{
			RunTest(@"<html><p tal:content=""null""></p></html>"
				, "<html><p></p></html>"
				, "Content of nothing did not evaluate to empty tag.");
		}

		[Test]
		public void TestContentDefault()
		{
			RunTest(@"<html><p tal:content=""default"">Original</p></html>"
				, "<html><p>Original</p></html>"
				, "Content of default did not evaluate to existing content");
		}

		[Test]
		public void TestContentString()
		{
			RunTest(@"<html><p tal:content=""test"">Original</p></html>"
				, "<html><p>testing</p></html>"
				, "Content of string did not evaluate to contain string");
		}

		[Test]
		public void TestContentExplicitText()
		{
			RunTest(@"<html><p tal:content=""text test"">Original</p></html>"
				, "<html><p>testing</p></html>"
				, "Content of string did not evaluate to contain string");
		}

		[Test]
		public void TestContentStructure()
		{
			Dictionary<string, object> weblog = new Dictionary<string, object>
            {
                { "subject", "Test subject" },
            };
			globals["weblog"] = weblog;
			string macros = @"<p metal:define-macro=""entry"">Some structure: <b tal:content='weblog[""subject""]'></b></p>";
			RunTest(macros + @"<html><p metal:use-macro='macros[""entry""]'>Original</p></html>"
				, "<html><p>Some structure: <b>Test subject</b></p></html>"
				, "Content of Structure did not evaluate to expected result");
		}

		[Test]
		public void TestTALDisabledContentStructure()
		{
			RunTest(@"<html><p tal:content='structure weblog[""entry""]'>Original</p></html>"
				, @"<html><p>Some structure: <b tal:content=""weblog/subject""></b></p></html>"
				, "Content of Structure did not evaluate to expected result");
		}

		[Test]
		public void TestTALContentEscaping()
		{
			RunTest(@"<html><p tal:content='string:=&=&amp;=&nbsp;=&lt;=&gt;=&quot;=""='>Original</p>=&=&amp;=&nbsp;=&lt;=&gt;=&quot;=""=</html>"
				, @"<html><p>=&amp;=&amp;=&nbsp;=&lt;=&gt;=""=""=</p>=&=&amp;=&nbsp;=&lt;=&gt;=&quot;=""=</html>"
				, "Escaped content did not evaluate to expected result");
		}

		[Test]
		public void TestMultilineTag()
		{
			RunTest(@"<html><p
	tal:content='string:str'>Original</p></html>"
				, @"<html><p>str</p></html>"
				, "Multiline tag did not evaluate to expected result");
		}
	}
}
