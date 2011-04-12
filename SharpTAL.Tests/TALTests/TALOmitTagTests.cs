using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;

using NUnit.Framework;

namespace SharpTAL.SharpTALTests.TALTests
{
	[TestFixture]
	public class TALOmitTagTests
	{
		public static SharpTAL.Interfaces.ITemplateCache cache;
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
			cache = new FileSystemTemplateCache(cacheFolder, true, typeof(TALOmitTagTests).Name + "_{key}.dll");
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
			globals.Add("link", "www.owlfish.com"); ;
		}

		public static void RunTest(string template, string expected, string errMsg)
		{
			RunTest(template, expected, errMsg, null);
		}

		public static void RunTest(string template, string expected, string errMsg,
			Dictionary<string, string> inlineTemplates)
		{
			string actual = cache.RenderTemplate(template, globals, inlineTemplates);
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
