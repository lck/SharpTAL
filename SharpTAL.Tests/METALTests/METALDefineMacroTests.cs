namespace SharpTAL.SharpTALTests.METALTests
{
	using System;
	using System.Text;
	using System.Collections.Generic;
	using System.Linq;
	using System.IO;
	using System.Reflection;

	using NUnit.Framework;

	using SharpTAL.TemplateCache;

	[TestFixture]
	public class METALDefineMacroTests
	{
		public static ITemplateCache cache;
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

		public static void RunTest(string template, string expected, string errMsg)
		{
			string actual = cache.RenderTemplate(template, globals);
			actual = actual.Replace("{", "{{").Replace("}", "}}");
			Assert.AreEqual(expected, actual, "{1} - {0}template: {2}{0}actual: {3}{0}expected: {4}",
				Environment.NewLine, errMsg, template, actual, expected);
		}

		[Test]
		public void TestSingleMacroDefinitionInRepeat()
		{
			string macros = @"<div metal:define-macro=""one"" class=""funny"">No slots here</div>";
			RunTest(macros + @"<html><body><div foo=""a"" tal:repeat=""i string:abc""><div metal:use-macro='macros[""one""]'></div></div></body></html>",
				@"<html><body><div foo=""a""><div class=""funny"">No slots here</div></div><div foo=""a""><div class=""funny"">No slots here</div></div><div foo=""a""><div class=""funny"">No slots here</div></div></body></html>",
				"Single macro in repeat with no slots failed.");
		}

		[Test]
		public void TestSingleMacroDefinition()
		{
			string macros = @"<div metal:define-macro=""one"" class=""funny"">No slots here</div>";
			RunTest(macros + @"<html><body metal:use-macro='macros[""one""]'><h1 metal:fill-slot=""title"">Expansion of macro one</h1></body></html>",
				@"<html><div class=""funny"">No slots here</div></html>",
				"Single macro with no slots failed.");
		}

		[Test]
		public void TestTwoMacroDefinition()
		{
			string macros = @"<body metal:define-macro=""two"">A second macro</body><div metal:define-macro=""one"" class=""funny"">No slots here</div>";
			RunTest(macros + @"<html><body metal:use-macro='macros[""one""]'><h1 metal:fill-slot=""title"">Expansion of macro one</h1></body></html>",
				@"<html><div class=""funny"">No slots here</div></html>",
				"Two macros with no slots failed.");
		}

		[Test]
		public void TestNestedMacroDefinition()
		{
			string macros = @"<div metal:define-macro=""two"" class=""funny""><body metal:define-macro=""one"">A second macro</body>No slots here</div>";
			RunTest(macros + @"<html><body metal:use-macro='macros[""one""]'><h1 metal:fill-slot=""title"">Expansion of macro one</h1></body></html>",
				"<html><body>A second macro</body></html>",
				"Nested macro with no slots failed.");
		}

		[Test]
		public void TestMacroTALDefinitionDefinedAtStart()
		{
			string macros = @"<p metal:define-macro=""one"" tal:content=""test"">Wibble</p>";
			RunTest(macros + @"<html><body metal:use-macro='macros[""one""]'><h1 metal:fill-slot=""title"">Expansion of macro one</h1></body></html>",
				"<html><p>testing</p></html>",
				"TAL Command on a macro failed.");
		}

		[Test]
		public void TestMacroDefinedInside()
		{
			string macros = @"<p metal:define-macro=""one"" tal:content=""test"">Wibble</p>";
			RunTest(@"<html>" + macros + @"<body metal:use-macro='macros[""one""]'><h1 metal:fill-slot=""title"">Expansion of macro one</h1></body></html>",
				"<html><p>testing</p></html>",
				"TAL Command on a macro failed.");
		}

		[Test]
		public void TestMacroDefinedAtEnd()
		{
			string macros = @"<p metal:define-macro=""one"" tal:content=""test"">Wibble</p>";
			RunTest(@"<html><body metal:use-macro='macros[""one""]'><h1 metal:fill-slot=""title"">Expansion of macro one</h1></body></html>" + macros,
				"<html><p>testing</p></html>",
				"TAL Command on a macro failed.");
		}
	}
}
