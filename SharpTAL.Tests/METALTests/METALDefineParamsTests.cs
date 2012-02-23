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
	public class METALDefineParamsTests
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
			globals.Add("link", "www.owlfish.com");
			globals.Add("needsQuoting", @"Does ""this"" & work?");
		}

		public static void RunTest(string macros, string template, string expected, string errMsg)
		{
			template += macros;
			string actual = new Template(template).Render(globals);
			actual = actual.Replace("{", "{{").Replace("}", "}}");
			Assert.AreEqual(expected, actual, "{1} - {0}template: {2}{0}actual: {3}{0}expected: {4}",
				Environment.NewLine, errMsg, template, actual, expected);
		}

		[Test]
		public void TestSingleParam()
		{
			RunTest(@"<div metal:define-macro=""one"" class=""funny"" metal:define-param=""string blue null"">Before <i tal:content=""blue"">blue</i> After</div>",
				@"<html><body metal:use-macro='macros[""one""]'>Nowt <i metal:fill-param='blue ""white""'/> here</body></html>",
				@"<html><div class=""funny"">Before <i>white</i> After</div></html>",
				"Single param definition failed.");
		}

		[Test]
		public void TestDoubleParams()
		{
			RunTest(@"<div metal:define-macro=""one"" class=""funny"">Before <i tal:content=""blue"">blue</i> <i tal:content=""red"">blue</i> After<tal:tag metal:define-param='string blue null;string red ""red""'/></div>",
				@"<html><body metal:use-macro='macros[""one""]'>Nowt <i metal:fill-param='blue ""white"";red ""orange""'/> here</body></html>",
				@"<html><div class=""funny"">Before <i>white</i> <i>orange</i> After</div></html>",
				"Double param definition failed.");
		}

		[Test]
		public void TestRecursiveMacroWithParams()
		{
			RunTest(@"<tal:tag metal:define-macro=""one"" metal:define-param=""int count 0""><i tal:replace=""string:Count=${count};"">100</i><tal:tag tal:define='nonlocal count count - 1' tal:condition='count &gt; 0'><tal:tag metal:use-macro='macros[""one""]' metal:fill-param='count count'/></tal:tag></tal:tag>",
				@"<body metal:use-macro='macros[""one""]'>Nowt <i metal:fill-param='count 5'/> here</body>",
				@"Count=5;Count=4;Count=3;Count=2;Count=1;",
				"Recursive macro with params failed.");
		}
	}
}
