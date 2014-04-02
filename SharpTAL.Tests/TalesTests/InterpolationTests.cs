using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace SharpTAL.Tests.TalesTests
{
	[TestFixture]
	public class InterpolationTests
	{
		public static void RunTest(string template, string expected, string errMsg)
		{
			string actual = new Template(template).Render(new Dictionary<string, object> { });
			actual = actual.Replace("{", "{").Replace("}", "}");
			Assert.AreEqual(expected, actual, "{1} - {0}template: {2}{0}actual: {3}{0}expected: {4}",
				Environment.NewLine, errMsg, template, actual, expected);
		}

		[Test]
		public void TestInterpolation()
		{
			RunTest(@"<html>${""Hello "" + string.Format(""{0}"", @""World"") + ""!""}</html>",
			   @"<html>Hello World!</html>",
			   "Interpolation failed!");
		}

		[Test]
		public void TestInterpolationDisabled()
		{
			RunTest(@"<html><div meta:interpolation=""false"">${""Hello ""}</div> ${string.Format(""{0}"", @""World"") + ""!""}</html>",
			   @"<html><div>${""Hello ""}</div> World!</html>",
			   "Interpolation failed!");
		}

		[Test]
		public void TestInterpolationDisabledOmmitedTag()
		{
			RunTest(@"<html><tal:tag meta:interpolation=""false"">${""Hello ""}</tal:tag> ${string.Format(""{0}"", @""World"") + ""!""}</html>",
			   @"<html>${""Hello ""} World!</html>",
			   "Interpolation failed!");
		}

		[Test]
		public void TestInterpolationInAttributes()
		{
			RunTest(@"<div class='${""Hello "" + string.Format(""{0}"", @""World"") + ""!""}'></div>",
			   @"<div class='Hello World!'></div>",
			   "Interpolation failed!");
		}

		[Test]
		public void TestInterpolationInAttributesDisabled()
		{
			RunTest(@"<html><div meta:interpolation=""false"" class='${""Hello ""}' /><div class='${string.Format(""{0}"", @""World"") + ""!""}' /></html>",
			   @"<html><div class='${""Hello ""}' /><div class='World!' /></html>",
			   "Interpolation failed!");
		}

		[Test]
		public void TestInterpolationMultiline()
		{
			RunTest(@"<html>${string:Hello "" { ${string.Format(""{0}"", @""
World"") + ""!""} }, \${escaped} 9 / 3 = ${9 / 3}}}</html>",
			   @"<html>Hello "" { 
World! }, \${escaped} 9 / 3 = 3}</html>",
			   "Multiline interpolation failed!");
		}

		[Test]
		public void TestInterpolationCDataMultiline()
		{
			RunTest(@"<html>
  <head>
    <script>
      <![CDATA[
      alert(""${""Hello world!""}"");
      ]]>
    </script>
  </head>
</html>",
			   @"<html>
  <head>
    <script>
      <![CDATA[
      alert(""Hello world!"");
      ]]>
    </script>
  </head>
</html>",

			   "CData interpolation failed!");
		}
	}
}
