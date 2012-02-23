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
	public class METALNameSpaceTests
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
			globals = new Dictionary<string, object>();
			globals.Add("test", "testing");
			globals.Add("one", new List<int>() { 1 });
			globals.Add("two", new List<string>() { "one", "two" });
			globals.Add("three", new List<object>() { 1, "Two", 3 });
		}

		public static void RunTest(string macros, string template, string expected, string errMsg)
		{
			template += macros;
			string actual = new Template(template).Render(globals);
			actual = actual.Replace("{", "{{").Replace("}", "}}");
			Assert.AreEqual(expected, actual, "{1} - {0}template: {2}{0}actual: {3}{0}expected: {4}",
				Environment.NewLine, errMsg, template, actual, expected);
		}

		// Test that rebinding the namespaces works		
		[Test]
		public void TestSingleBindNoCommands()
		{
			RunTest(@"",
				@"<html xmlns:newmetal=""http://xml.zope.org/namespaces/metal""><body metal:use-macro=""macros[&quot;one&quot;]"">Nowt <i metal:fill-slot=""blue"">white</i> here</body></html>",
				@"<html><body metal:use-macro=""macros[&quot;one&quot;]"">Nowt <i metal:fill-slot=""blue"">white</i> here</body></html>",
				"Single Bind, commands, failed.");
		}

		[Test]
		public void TestSingleBindCommands()
		{
			RunTest(@"<div xmlns:newmetal=""http://xml.zope.org/namespaces/metal"" newmetal:define-macro=""one"" class=""funny"">Before <b newmetal:define-slot=""blue"">blue</b> After</div>",
				@"<html xmlns:newmetal=""http://xml.zope.org/namespaces/metal""><body newmetal:use-macro='macros[""one""]'>Nowt <i newmetal:fill-slot=""blue"">white</i> here</body></html>",
				@"<html><div class=""funny"">Before <i>white</i> After</div></html>",
				"Single Bind, commands, failed.");
		}

		// Test to ensure that using elements in the metal namespace omits tags
		[Test]
		public void TestMETALEmlement()
		{
			RunTest(@"<newmetal:div xmlns:newmetal=""http://xml.zope.org/namespaces/metal"" newmetal:define-macro=""one"" class=""funny"">Before <b newmetal:define-slot=""blue"">blue</b> After</newmetal:div>",
				@"<html xmlns:newmetal=""http://xml.zope.org/namespaces/metal""><body newmetal:use-macro='macros[""one""]'>Nowt <newmetal:block newmetal:fill-slot=""blue"">white</newmetal:block> here</body></html>",
				@"<html>Before white After</html>",
				"METAL namespace does not cause implicit omit-tag");
		}
	}
}
