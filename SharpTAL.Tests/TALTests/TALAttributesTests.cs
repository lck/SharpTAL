namespace SharpTAL.SharpTALTests.TALTests
{
	using System;
	using System.Text;
	using System.Collections.Generic;
	using System.Linq;
	using System.IO;
	using System.Reflection;
	using System.Globalization;

	using NUnit.Framework;

	using SharpTAL.TemplateCache;

	[TestFixture]
	public class TALAttributesTests
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
			globals.Add("anotherdefault", new Dictionary<string, string>() { { "inhere", Constants.DEFAULT_VALUE } });
		}

		public static void RunTest(string template, string expected, string errMsg)
		{
			string actual = cache.RenderTemplate(template, globals);
			Assert.AreEqual(expected, actual, "{1} - {0}template: {2}{0}actual: {3}{0}expected: {4}",
				Environment.NewLine, errMsg, template, actual, expected);
		}

		[Test]
		public void TestHTMLNoValueAttribute()
		{
			RunTest(
				@"<html link>Hello</html>",
				@"<html link>Hello</html>",
				"HTML empty attribute failed.");
		}

		[Test]
		public void TestHTMLSingletonTagAttribute()
		{
			RunTest(
				@"<html tal:attributes=""link link"" href=""owlfish.com"">",
				@"<html href=""owlfish.com"" link=""www.owlfish.com"">",
				"Addition of attribute 'link' failed.");
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

		[Test]
		public void TestMultilineAttribute()
		{
			RunTest(
				@"<html tal:attributes=""link link;" + "\n" + @"test test"" href=""owlfish.com"">Hello</html>",
				@"<html href=""owlfish.com"" link=""www.owlfish.com"" test=""testing"">Hello</html>",
				"Addition of attribute 'link' failed.");
		}

		[Test]
		public void TestMultipleTalAttributeJoinAndOverride()
		{
			RunTest(
				@"<html tal:attributes=""link link"" href=""owlfish.com"" tal:attributes=""link string:${1+1}"">Hello</html>",
				@"<html href=""owlfish.com"" link=""2"">Hello</html>",
				"Multiple attribute join and override failed.");
		}

		[Test]
		public void TestInlineExpressions()
		{
			RunTest(
				@"<html title='1 + 1 = ${1 + 1}'>Hello</html>",
				@"<html title='1 + 1 = 2'>Hello</html>",
				"Inline expressions escaping failed.");
		}

		[Test]
		public void TestInlineExpressionsWithFormat()
		{
			RunTest(
				@"<html title='${string.Format(""1 + 1 = {0}"", 1 + 1)}'>Hello</html>",
				@"<html title='1 + 1 = 2'>Hello</html>",
				"Inline expressions with format failed.");
		}

		[Test]
		public void TestInlineExpressionsWithExprEscape()
		{
			RunTest(
				@"<html title='This is \${escaped}, ${string.Format(""1 + 1 = {0}"", 1 + 1)}'>Hello</html>",
				@"<html title='This is \${escaped}, 1 + 1 = 2'>Hello</html>",
				"Inline expressions with expression escaping failed.");
		}

		[Test]
		public void TestInlineExpressionsWithQuoteEscaping()
		{
			RunTest(
				@"<html tal:define='quote string:[""]' tal:define=""quote2 string:[']"" tal:attributes=""class string:the_guote_${quote}"" title=""this is escaped: ${quote} and this is not: ${quote2}"" id='the unescaped quote ${quote}'>Hello</html>",
				@"<html title=""this is escaped: [&quot;] and this is not: [']"" id='the unescaped quote [""]' class=""the_guote_[&quot;]"">Hello</html>",
				"Inline expressions quote escaping failed.");
		}

		[Test]
		public void TestAttributeInvariantCulture()
		{
			string template = @"<html tal:attributes=""value 1.05""> </html>";
			string expected = @"<html value=""1.05""> </html>";
			string actual = cache.RenderTemplate(template, globals);
			Assert.AreEqual(expected, actual, "{1} - {0}template: {2}{0}actual: {3}{0}expected: {4}",
				Environment.NewLine, "Conversion using invariant culture failed", template, actual, expected);
		}

		[Test]
		public void TestAttributeLocalCulture()
		{
			string template = @"<html tal:attributes=""value 1.05""> </html>";
			string expected = @"<html value=""1,05""> </html>";
			TemplateInfo ti;
			string actual = cache.RenderTemplate(template, globals, null, out ti, new CultureInfo("fi-FI"));
			Assert.AreEqual(expected, actual, "{1} - {0}template: {2}{0}actual: {3}{0}expected: {4}",
				Environment.NewLine, "Conversion using invariant culture failed", template, actual, expected);
		}
	}
}
