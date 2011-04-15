using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace SharpTAL.SharpTALTests.METALTests
{
	[TestFixture]
	public class METALNameSpaceTests
	{
		public static SharpTAL.ITemplateCache cache;
		public static Dictionary<string, object> globals;

		[TestFixtureSetUp]
		public void SetUpClass()
		{
			// Using MemoryTemplateCache in this tests
			cache = new MemoryTemplateCache();
		}

		[TestFixtureTearDown]
		public void CleanupClass()
		{
		}

		[SetUp]
		public void SetUp()
		{
			globals = new Dictionary<string, object>();
			globals.Add("test", "testing");
			globals.Add("one", new List<int>() { 1 });
			globals.Add("two", new List<string>() { "one", "two" });
			globals.Add("three", new List<object>() { 1, "Two", 3 });
		}

		public static void RunTest(string template, string expected, string errMsg,
			Dictionary<string, string> inlineTemplates)
		{
			string actual = cache.RenderTemplate(template, globals, inlineTemplates);
			actual = actual.Replace("{", "{{").Replace("}", "}}");
			Assert.AreEqual(expected, actual, "{1} - {0}template: {2}{0}actual: {3}{0}expected: {4}",
				Environment.NewLine, errMsg, template, actual, expected);
		}

		public static void RunTest(string macros, string template, string expected, string errMsg)
		{
			Dictionary<string, string> inlineTemplates = new Dictionary<string, string>();
			inlineTemplates.Add("site", macros);
			RunTest(template, expected, errMsg, inlineTemplates);
		}

		// Test that rebinding the namespaces works		
		[Test]
		public void TestSingleBindNoCommands()
		{
			RunTest(@"<html xmlns:newmetal=""http://xml.zope.org/namespaces/metal""><div metal:define-macro=""one"" class=""funny"">Before <b metal:define-slot=""blue"">blue</b> After</div></html>",
				@"<html xmlns:newmetal=""http://xml.zope.org/namespaces/metal""><body metal:use-macro=""site.macros[&quot;one&quot;]"">Nowt <i metal:fill-slot=""blue"">white</i> here</body></html>",
				@"<html><body metal:use-macro=""site.macros[&quot;one&quot;]"">Nowt <i metal:fill-slot=""blue"">white</i> here</body></html>",
				"Single Bind, commands, failed.");
		}

		[Test]
		public void TestSingleBindCommands()
		{
			RunTest(@"<html xmlns:newmetal=""http://xml.zope.org/namespaces/metal""><div newmetal:define-macro=""one"" class=""funny"">Before <b newmetal:define-slot=""blue"">blue</b> After</div></html>",
				@"<html xmlns:newmetal=""http://xml.zope.org/namespaces/metal""><body newmetal:use-macro='site.macros[""one""]'>Nowt <i newmetal:fill-slot=""blue"">white</i> here</body></html>",
				@"<html><div class=""funny"">Before <i>white</i> After</div></html>",
				"Single Bind, commands, failed.");
		}

		// Test to ensure that using elements in the metal namespace omits tags
		[Test]
		public void TestMETALEmlement()
		{
			RunTest(@"<html xmlns:newmetal=""http://xml.zope.org/namespaces/metal""><newmetal:div newmetal:define-macro=""one"" class=""funny"">Before <b newmetal:define-slot=""blue"">blue</b> After</newmetal:div></html>",
				@"<html xmlns:newmetal=""http://xml.zope.org/namespaces/metal""><body newmetal:use-macro='site.macros[""one""]'>Nowt <newmetal:block newmetal:fill-slot=""blue"">white</newmetal:block> here</body></html>",
				@"<html>Before white After</html>",
				"METAL namespace does not cause implicit omit-tag");
		}
	}
}
