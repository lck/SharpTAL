using System;
using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;

namespace SharpTAL.Tests.TalTests
{
	[TestFixture]
	public class TalOnErrorTests
	{
		public static Dictionary<string, object> globals;

		public class SampleClass
		{
			public int ThrowException()
			{
				throw new Exception();
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
			globals = new Dictionary<string, object>();
			globals.Add("sample", new SampleClass());
		}

		public static void RunTest(string template, string expected, string errMsg)
		{
			string actual = new Template(template).Render(globals);
			actual = actual.Replace("{", "{{").Replace("}", "}}");
			Assert.AreEqual(expected, actual, "{1} - {0}template: {2}{0}actual: {3}{0}expected: {4}",
				Environment.NewLine, errMsg, template, actual, expected);
		}

		[Test]
		public void TestOnErrorString()
		{
			RunTest(@"<html><p tal:content=""sample.ThrowException()"" tal:on-error=""string:Error message"">Original</p></html>"
				, "<html><p>Error message</p></html>"
				, "Content of error did not evaluate to contain error message");
		}
	}
}
