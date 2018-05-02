using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace SharpTAL.Tests.TalesTests
{
	[TestFixture]
	public class TalesStringTests
	{
		public static Dictionary<string, object> globals;

		private delegate object TestFuncDelegate();

		private static object simpleFunction()
		{
			return "Hello";
		}

		private static object simpleFalseFunc()
		{
			return 0;
		}

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
			globals.Add("top", "Hello from the top");
			globals.Add("alt", "Wobble the way");
			globals.Add("holder", new Dictionary<string, object>() {
                { "helloFunc", new TestFuncDelegate(simpleFunction) },
                { "falseFunc", new TestFuncDelegate(simpleFalseFunc) }
            });
			globals.Add("version", 31);
			globals.Add("uniString", "Hello");
		}

		public static void RunTest(string template, string expected, string errMsg)
		{
			string actual = new Template(template).Render(globals);
			actual = actual.Replace("{", "{").Replace("}", "}");
			Assert.AreEqual(expected, actual, "{1} - {0}template: {2}{0}actual: {3}{0}expected: {4}",
				Environment.NewLine, errMsg, template, actual, expected);
		}

		[Test]
		public void TestEmptyString()
		{
			RunTest(@"<html tal:content=""string:"">Exists</html>",
			   "<html></html>",
			   "Empty string returned something!");
		}

		[Test]
		public void TestStaticString()
		{
			RunTest(@"<html tal:content=""string:Hello World!"">Exists</html>",
			   "<html>Hello World!</html>",
			   "Static string didnt appear!");
		}

		[Test]
		public void TestSingleVariable()
		{
			RunTest(@"<html tal:content=""string:${top}"">Exists</html>",
			   "<html>Hello from the top</html>",
			   "Single variable failed!");
		}

		[Test]
		public void TestStartVariable()
		{
			RunTest(@"<html tal:content=""string:${top} of here"">Exists</html>",
			   "<html>Hello from the top of here</html>",
			   "Start variable failed!");
		}

		[Test]
		public void TestMidVariable()
		{
			RunTest(@"<html tal:content=""string:Thoughts - ${top} eh?"">Exists</html>",
			   "<html>Thoughts - Hello from the top eh?</html>",
			   "Mid variable failed!");
		}

		[Test]
		public void TestEndVariable()
		{
			RunTest(@"<html tal:content=""string:Thought - ${top}"">Exists</html>",
			   "<html>Thought - Hello from the top</html>",
			   "End variable failed!");
		}

		[Test]
		public void TestNumericVariable()
		{
			RunTest(@"<html tal:content=""string:Thought - ${version}"">Exists</html>",
			   "<html>Thought - 31</html>",
			   "Numeric test variable failed!");
		}

		[Test]
		public void TestUnicodeVariable()
		{
			RunTest(@"<html tal:content=""string:Thought - ${uniString}"">Exists</html>",
			   "<html>Thought - Hello</html>",
			   "Unicode test variable failed!");
		}

		[Test]
		public void TestSinglePath()
		{
			RunTest(@"<html tal:content=""string:${top}"">Exists</html>",
			   "<html>Hello from the top</html>",
			   "Single path failed!");
		}

		[Test]
		public void TestStartPath()
		{
			RunTest(@"<html tal:content=""string:${top} of here"">Exists</html>",
			   "<html>Hello from the top of here</html>",
			   "Start path failed!");
		}

		[Test]
		public void TestMidPath()
		{
			RunTest(@"<html tal:content=""string:Thoughts - ${top}eh?"">Exists</html>",
			   "<html>Thoughts - Hello from the topeh?</html>",
			   "Mid path failed!");
		}

		[Test]
		public void TestEndPath()
		{
			RunTest(@"<html tal:content=""string:Thought - ${top}"">Exists</html>",
			   "<html>Thought - Hello from the top</html>",
			   "End path failed!");
		}

		[Test]
		public void TestMultiplePath()
		{
			RunTest(@"<html tal:content=""string:Thought - ${top} is here and ${string:recursive}"">Exists</html>",
			   "<html>Thought - Hello from the top is here and recursive</html>",
			   "Multiple paths failed!");
		}

		[Test]
		public void TestNoSuchPath()
		{
			Assert.Throws<CompileSourceException>(() => RunTest(@"<html tal:content=""string:${no.such.path}"">Exists</html>",
                                                                "<html></html>",
                                                                "No such path failed!"));
		}

		[Test]
		public void TestPartialMissing()
		{
			Assert.Throws<CompileSourceException>(() => RunTest(@"<html tal:content=""string: First bit here: ${alt} second bit not: ${nosuchname} there."">Exists</html>",
                                                                "<html>First bit here: Wobble the way second bit not:  there.</html>",
                                                                "Combination of non-existant variable and existing test failed!"));
		}
	}
}
