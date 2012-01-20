namespace SharpTAL.SharpTALTests.TALTests
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
	public class TALConditionTests
	{
		public static ITemplateCache cache;
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
			cache = new FileSystemTemplateCache(cacheFolder, true, typeof(TALConditionTests).Name + "_{key}.dll");
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
			globals.Add("one", new List<object>() { 1 });
			globals.Add("two", new List<object>() { "one", "two" });
			globals.Add("three", new List<object>() { 1, "Two", 3 });
		}

		public static void RunTest(string template, string expected, string errMsg)
		{
			string actual = cache.RenderTemplate(template, globals);
			Assert.AreEqual(expected, actual, "{1} - {0}template: {2}{0}actual: {3}{0}expected: {4}",
				Environment.NewLine, errMsg, template, actual, expected);
		}

		[Test]
		public void TestConditionDefault()
		{
			RunTest(
				@"<html tal:condition=""default"">Hello</html>",
				"<html>Hello</html>",
				"Condition 'default' did not evaluate to true");
		}

		[Test]
		public void TestConditionExists()
		{
			RunTest(
				@"<html tal:condition=""test"">Hello</html>",
				"<html>Hello</html>",
				"Condition for something that exists evaluated false");
		}

		[Test]
		public void TestConditionNull()
		{
			RunTest(
				@"<html tal:condition=""null"">Hello</html>",
				"",
				"Condition null evaluated to true");
		}
	}
}
