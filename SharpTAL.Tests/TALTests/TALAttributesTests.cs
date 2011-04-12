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
	public class TALAttributesTests
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
			cache = new FileSystemTemplateCache(cacheFolder, true, typeof(TALAttributesTests).Name + "_{key}.dll");
		}

		[TestFixtureTearDown]
		public void CleanupClass()
		{
		}

		[SetUp]
		public void SetUp()
		{
			globals = new Dictionary<string, object>();
			globals.Add("empty", "");
			globals.Add("test", "testing");
			globals.Add("link", "www.owlfish.com");
			globals.Add("needsQuoting", @"Does ""this"" work?");
			globals.Add("number", 5);
			globals.Add("uniQuote", @"Does ""this"" work?");
			globals.Add("anotherdefault", new Dictionary<string, string>() { { "inhere", Constants.DEFAULTVALUE } });
		}

		public static void RunTest(string template, string expected, string errMsg)
		{
			string actual = cache.RenderTemplate(template, globals);
			Assert.AreEqual(expected, actual, "{1} - {0}template: {2}{0}actual: {3}{0}expected: {4}",
				Environment.NewLine, errMsg, template, actual, expected);
		}

		[Test]
		public void TestEmptyAttribute()
		{
			RunTest(
				@"<html link="""">Hello</html>",
				@"<html link="""">Hello</html>",
				"Empty attribute failed.");
		}

		[Test]
		public void TestAddingAnAttribute()
		{
			RunTest(
				@"<html tal:attributes=""link link"" href=""owlfish.com"">Hello</html>",
				@"<html href=""owlfish.com"" link=""www.owlfish.com"">Hello</html>",
				"Addition of attribute 'link' failed.");
		}

		[Test]
		public void TestRemovingAnAttribute()
		{
			RunTest(
				@"<html class=""test"" tal:attributes=""href null"" href=""owlfish.com"">Hello</html>",
				@"<html class=""test"">Hello</html>",
				"Removal of attribute \"href\" failed.");
		}

		[Test]
		public void TestDefaultAttribute()
		{
			RunTest(
				@"<html class=""test"" tal:attributes=""href default"" href=""owlfish.com"">Hello</html>",
				@"<html class=""test"" href=""owlfish.com"">Hello</html>",
				"Defaulting of attribute \"href\" failed.");
		}

		[Test]
		public void TestAnotherDefaultAttribute()
		{
			RunTest(
				@"<html class=""test"" tal:attributes='href anotherdefault[""inhere""]' href=""owlfish.com"">Hello</html>",
				@"<html class=""test"" href=""owlfish.com"">Hello</html>",
				"Defaulting of attribute \"href\" failed.");
		}

		[Test]
		public void TestMultipleAttributes()
		{
			RunTest(
				@"<html old=""still here"" class=""test"" tal:attributes=""href default;class null;new test"" href=""owlfish.com"">Hello</html>",
				@"<html old=""still here"" new=""testing"" href=""owlfish.com"">Hello</html>",
				"Setting multiple attributes at once failed.");
		}

		[Test]
		public void TestMultipleAttributesSpace()
		{
			RunTest(
				@"<html old=""still here"" class=""test"" tal:attributes=""href default ; class string:Hello there; new test"" href=""owlfish.com"">Hello</html>",
				@"<html old=""still here"" class=""Hello there"" href=""owlfish.com"" new=""testing"">Hello</html>",
				"Setting multiple attributes at once, with spaces between semi-colons, failed.");
		}

		[Test]
		public void TestMultipleAttributesEscaped()
		{
			RunTest(
				@"<html old=""still &quot; here"" class=""test"" tal:attributes=""href default ; class string: Semi-colon;;test;new test "" href=""owlfish.com"">Hello</html>",
				@"<html old=""still &quot; here"" class="" Semi-colon;test"" href=""owlfish.com"" new=""testing"">Hello</html>",
				"Setting multiple attributes at once, with spaces between semi-colons, failed.");
		}

		[Test]
		public void TestAttributeEscaping()
		{
			RunTest(
				@"<html existingatt=""&quot;Testing&quot;"" tal:attributes=""href needsQuoting"">Hello</html>"
				, @"<html existingatt=""&quot;Testing&quot;"" href=""Does &quot;this&quot; work?"">Hello</html>"
				, "Escaping of new attributes failed.");
		}

		[Test]
		public void TestNumberAttributeEscaping()
		{
			RunTest(
				@"<html existingatt=""&quot;Testing&quot;"" tal:attributes=""href number"">Hello</html>"
				, @"<html existingatt=""&quot;Testing&quot;"" href=""5"">Hello</html>"
				, "Escaping of new attributes failed.");
		}

		[Test]
		public void TestNumberAttributeEscaping2()
		{
			RunTest(
				@"<html existingatt=""&quot;Testing&quot;"" tal:attributes=""href uniQuote"">Hello</html>"
				, @"<html existingatt=""&quot;Testing&quot;"" href=""Does &quot;this&quot; work?"">Hello</html>"
				, "Escaping of new attributes failed.");
		}
	}
}
