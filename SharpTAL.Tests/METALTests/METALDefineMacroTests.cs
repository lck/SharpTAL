using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;

using NUnit.Framework;

namespace SharpTAL.SharpTALTests.METALTests
{
	[TestFixture]
	public class METALDefineMacroTests
	{
		public static SharpTAL.Interfaces.ITemplateCache cache;
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
			globals.Add("link", "www.owlfish.com");
			globals.Add("needsQuoting", @"Does ""this"" work?");
		}

		public static void RunTest(string template, string expected, string errMsg,
			Dictionary<string, string> inlineTemplates)
		{
			string actual = cache.RenderTemplate(template, globals, inlineTemplates);
			actual = actual.Replace("{", "{{").Replace("}", "}}");
			Assert.AreEqual(expected, actual, "{1} - {0}template: {2}{0}actual: {3}{0}expected: {4}",
				Environment.NewLine, errMsg, template, actual, expected);
		}

		public static void RunTest(string template, string expected, string errMsg)
		{
			Dictionary<string, string> inlineTemplates = new Dictionary<string, string>();
			inlineTemplates.Add("site", template);
			string pageTemplate = @"<html><body metal:use-macro='site.macros[""one""]'><h1 metal:fill-slot=""title"">Expansion of macro one</h1></body></html>";
			RunTest(pageTemplate, expected, errMsg, inlineTemplates);
		}

		public static void RunTest2(string template, string expected, string errMsg)
		{
			Dictionary<string, string> inlineTemplates = new Dictionary<string, string>();
			inlineTemplates.Add("site", template);
			string pageTemplate2 = @"<html><body><div foo=""a"" tal:repeat=""i string:abc""><div metal:use-macro='site.macros[""one""]'></div></div></body></html>";
			RunTest(pageTemplate2, expected, errMsg, inlineTemplates);
		}

		[Test]
		public void TestSingleMacroDefinitionInRepeat()
		{
			RunTest2(@"<html><div metal:define-macro=""one"" class=""funny"">No slots here</div></html>",
				@"<html><body><div foo=""a""><div class=""funny"">No slots here</div></div><div foo=""a""><div class=""funny"">No slots here</div></div><div foo=""a""><div class=""funny"">No slots here</div></div></body></html>",
				"Single macro in repeat with no slots failed.");
		}

		[Test]
		public void TestSingleMacroDefinition()
		{
			RunTest(@"<html><div metal:define-macro=""one"" class=""funny"">No slots here</div></html>",
				@"<html><div class=""funny"">No slots here</div></html>",
				"Single macro with no slots failed.");
		}

		[Test]
		public void TestTwoMacroDefinition()
		{
			RunTest(@"<html><body metal:define-macro=""two"">A second macro</body><div metal:define-macro=""one"" class=""funny"">No slots here</div></html>",
				@"<html><div class=""funny"">No slots here</div></html>",
				"Two macros with no slots failed.");
		}

		[Test]
		public void TestNestedMacroDefinition()
		{
			RunTest(@"<html><div metal:define-macro=""two"" class=""funny""><body metal:define-macro=""one"">A second macro</body>No slots here</div></html>",
				"<html><body>A second macro</body></html>",
				"Nested macro with no slots failed.");
		}

		[Test]
		public void TestMacroTALDefinition()
		{
			RunTest(@"<html><p metal:define-macro=""one"" tal:content=""test"">Wibble</p></html>",
				"<html><p>testing</p></html>",
				"TAL Command on a macro failed.");
		}
	}
}
