using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using NUnit.Framework;

namespace SharpTAL.SharpTALTests.TALTests
{
	[TestFixture]
	public class TALDefineTests
	{
		public static SharpTAL.ITemplateCache cache;
		public static Dictionary<string, object> globals;

		[TestFixtureSetUp]
		public void SetUpClass()
		{
			// Using FileSystemTemplateCache in this tests
			string cacheFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Template Cache");
			if (!Directory.Exists(cacheFolder))
			{
				Directory.CreateDirectory(cacheFolder);
			}
			cache = new FileSystemTemplateCache(cacheFolder, true, typeof(TALDefineTests).Name + "_{key}.dll");
		}

		[TestFixtureTearDown]
		public void CleanupClass()
		{
		}

		[SetUp]
		public void SetUp()
		{
			globals = new Dictionary<string, object>();
			globals.Add("test", "testing"); ;
			globals.Add("one", new List<object>() { 1 });
			globals.Add("two", new List<object>() { "one", "two" });
			globals.Add("three", new List<object>() { 1, "Two", 3 });
		}

		public static void RunTest(string template, string expected, string errMsg)
		{
			TemplateInfo ti;
			try
			{
				string actual = cache.RenderTemplate(template, globals, null, out ti);
				actual = actual.Replace("{", "{{").Replace("}", "}}");
				Assert.AreEqual(expected, actual, "{1} - {0}template: {2}{0}actual: {3}{0}expected: {4}",
					Environment.NewLine, errMsg, template, actual, expected);
			}
			finally
			{
			}
		}

		[Test]
		public void TestDefineString()
		{
			RunTest(@"<html tal:define=""def1 test""><p tal:content=""def1""></p></html>",
				"<html><p>testing</p></html>", "Simple string define failed.");
		}

		[Test]
		public void TestDefineList()
		{
			RunTest(@"<html tal:define=""def1 two""><p tal:repeat=""var def1"">Hello <b tal:content=""var""></b></p></html>",
				"<html><p>Hello <b>one</b></p><p>Hello <b>two</b></p></html>", "List define failed.");
		}

		[Test]
		public void TestDefineGlobal()
		{
			RunTest(@"<html><p tal:define=""global def1 test""></p><p tal:content=""def1""></p></html>"
				, "<html><p></p><p>testing</p></html>", "Global did not set globally");
		}

		[Test]
		public void TestDefineGlobalMultiple()
		{
			RunTest(@"<html><p tal:define='global def1 ""a""; global def1 ""b""; global def1 test'></p><p tal:content=""def1""></p></html>"
				, "<html><p></p><p>testing</p></html>", "Global did not set globally");
		}

		[Test]
		public void TestDefineSetLocalMultiple()
		{
			RunTest(@"<html><p tal:define='def1 ""a""; set def1 ""b""; set def1 test'><p tal:content=""def1""></p></p></html>"
				, "<html><p><p>testing</p></p></html>", "Local did not set");
		}

		[Test]
		public void TestDefineDefault()
		{
			RunTest(@"<html><p tal:define=""global test default""></p><p tal:content=""test"">Can you see me?</p></html>"
				, "<html><p></p><p>Can you see me?</p></html>", "Default variable did not define proplerly.");
		}

		[Test]
		public void TestDefineNothing()
		{
			RunTest(@"<html><p tal:define=""global test null""></p><p tal:content=""test"">Can you see me?</p></html>"
				, "<html><p></p><p></p></html>", "Nothing variable did not define proplerly.");
		}

		[Test]
		public void TestDefineMultipleLocal()
		{
			RunTest(@"<html><div tal:define=""firstVar test;secondVar string:This is a semi;;colon;thirdVar string:Test""><p tal:content=""test"">Testing</p><p tal:content=""secondVar""></p><p tal:content=""thirdVar""></p></div></html>"
				, "<html><div><p>testing</p><p>This is a semi;colon</p><p>Test</p></div></html>", "Multiple defines failed.");
		}

		[Test]
		public void TestDefineMultipleMixed()
		{
			RunTest(@"<html><div tal:define=""firstVar test;global secondVar string:This is a semi;;colon;thirdVar string:Test""><p tal:content=""test"">Testing</p><p tal:content=""secondVar""></p><p tal:content=""thirdVar""></p></div><b tal:content=""secondVar""></b></html>"
				, "<html><div><p>testing</p><p>This is a semi;colon</p><p>Test</p></div><b>This is a semi;colon</b></html>", "Multiple defines failed.");
		}

		[Test]
		public void TestDefineMultipleLocalRef()
		{
			RunTest(@"<html><div tal:define=""firstVar test;secondVar firstVar""><p tal:content=""test"">Testing</p><p tal:content=""secondVar""></p></div></html>"
				, "<html><div><p>testing</p><p>testing</p></div></html>", "Multiple local defines with references to earlier define failed.");
		}

		[Test]
		public void TestDefineInOmitScope()
		{
			RunTest(@"<html><div tal:define=""firstVar test""><tal:omit-scope tal:define=""secondVar firstVar""/><p tal:content=""test"">Testing</p><p tal:content=""secondVar""></p></div></html>"
				, "<html><div><p>testing</p><p>testing</p></div></html>", "Define in tal:omit-scope failed.");
		}
	}
}
