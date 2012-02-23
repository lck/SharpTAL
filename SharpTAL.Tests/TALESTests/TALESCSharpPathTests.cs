namespace SharpTAL.SharpTALTests.TALESTests
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
	public class TALESCSharpPathTests
	{
		public static Dictionary<string, object> globals;

		public delegate object TestFuncDelegate(string param);
		public delegate object TestFuncDelegate2();

		private static object simpleFunction(object param)
		{
			return string.Format("Hello {0}", param);
		}

		private static object helloFunction()
		{
			return "Hello";
		}

		public class TestingException : Exception
		{
			public TestingException()
				: base()
			{
			}

			public TestingException(string message)
				: base(message)
			{
			}
		}

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
		}

		public static void RunTest(string template, string expected, string errMsg)
		{
			globals = new Dictionary<string, object>();
			globals.Add("top", "Hello from the top");
			globals.Add("helloFunc", new TestFuncDelegate(simpleFunction));
			globals.Add("helloFunction", new TestFuncDelegate2(helloFunction));
			globals.Add("myList", new List<int>() { 1, 2, 3, 4, 5, 6 });
			globals.Add("testing", "testing");
			globals.Add("map", new Dictionary<string, object>() { { "test", "maptest" } });
			globals.Add("data", new Dictionary<string, object>() { { "one", 1 }, { "zero", 0 } });
			string actual = new Template(template).Render(globals);
			actual = actual.Replace("{", "{{").Replace("}", "}}");
			Assert.AreEqual(expected, actual, "{1} - {0}template: {2}{0}actual: {3}{0}expected: {4}",
				Environment.NewLine, errMsg, template, actual, expected);
		}

		[Test]
		public void TestCSharpPathFuncSuccess()
		{
			RunTest(@"<html tal:content='csharp:helloFunc (""Colin!"")'>Exists</html>",
				"<html>Hello Colin!</html>",
				"CSharp path with function failed.");
		}

		[Test]
		public void TestCSharpPathSliceSuccess()
		{
			RunTest(@"<html tal:repeat=""num csharp:myList.GetRange(2, 2)"" tal:content=""num"">Exists</html>",
			   "<html>3</html><html>4</html>",
			   "CSharp path with slice failed.");
		}

		[Test]
		public void TestCSharpPathArrayCreate()
		{
			RunTest(@"<html tal:repeat=""i csharp:from i in myList.GetRange(0, myList.Count - 1) where i &lt; 3 select i"" tal:content=""i"">Exists</html>",
			   "<html>1</html><html>2</html>",
			   "CSharp path with array create with slice failed.");
		}

		[Test]
		public void TestCSharpStringCompare()
		{
			RunTest(@"<html tal:condition='csharp: testing==""testing""'>Passed.</html>",
				"<html>Passed.</html>",
				"CSharp string compare failed.");
		}

		[Test]
		public void TestSystemString()
		{
			RunTest(@"<html tal:condition='csharp: ""abc"" == string.Format(""{0}{1}{2}"", ""a"", ""b"", ""c"")'>Passed.</html>",
				"<html>Passed.</html>",
				"CSharp string compare failed.");
		}

		[Test]
		public void TestSystemDateTime()
		{
			string year = System.DateTime.Now.ToString("yyyy");
			RunTest(@"<html tal:content='csharp: DateTime.Now.ToString(""yyyy"")'></html>",
				"<html>" + year + "</html>",
				"CSharp string compare failed.");
		}
	}
}
