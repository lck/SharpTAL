using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace SharpTAL.Tests.MetalTests
{
	[TestFixture]
	public class MetalImportTests
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
		}

		public static void RunTest(string template, string expected, string errMsg)
		{
			string actual = new Template(template).Render(globals);
			actual = actual.Replace("{", "{{").Replace("}", "}}");
			Assert.AreEqual(expected, actual, "{1} - {0}template: {2}{0}actual: {3}{0}expected: {4}",
				Environment.NewLine, errMsg, template, actual, expected);
		}

		[Test]
		public void TestImportDefaultNs()
		{
			RunTest(@"<html><div metal:import=""METALTests/Imports/Imports 1.html""><i metal:use-macro='macros[""Macro1""]'>Now</i></div></html>",
				@"<html><div>Macro1</div></html>",
				"Import macros to default namespace failed.");
		}

		[Test]
		public void TestImportRecursiveDefaultNs()
		{
			RunTest(@"<html><div><metal:tag import=""METALTests/Imports/Imports 1.html"" use-macro='macros[""Macro2""]'>Now</metal:tag></div></html>",
				@"<html><div>Macro2</div></html>",
				"Recursive import macros to default namespace failed.");
		}

		[Test]
		public void TestImportCustomNs()
		{
			RunTest(@"<html><div metal:import=""imp1:METALTests/Imports/Imports 1.html""><i metal:use-macro='imp1.macros[""Macro1""]'>Now</i></div></html>",
				@"<html><div>Macro1</div></html>",
				"Import macros to custom namespace failed.");
		}

		[Test]
		public void TestMultipleImportOneCustomNs()
		{
			RunTest(@"<html><div metal:import=""imp1:METALTests/Imports/Imports 1.html;imp1:METALTests/Imports/Imports 2.html""><i metal:use-macro='imp1.macros[""Macro2""]'>Now</i></div></html>",
				    @"<html><div>Macro2</div></html>",
				    "Multiple import macros to one custom namespace failed.");
		}

		[Test]
		public void TestMultipleImportMultipleCustomNs()
		{
			RunTest(@"<html><div><i metal:import=""imp1:METALTests/Imports/Imports 1.html;imp2:METALTests/Imports/Imports 2.html"" metal:use-macro='imp2.macros[""Macro2""]'>Now</i></div></html>",
				@"<html><div>Macro2</div></html>",
				"Multiple import macros to multiple custom namespace failed.");
		}
	}
}
