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
	public class CodeBlocksTests
	{
		public static void RunTest(string template, string expected, string errMsg)
		{
			string actual = new Template(template).Render(new Dictionary<string, object> { });
			actual = actual.Replace("{", "{").Replace("}", "}");
			Assert.AreEqual(expected, actual, "{1} - {0}template: {2}{0}actual: {3}{0}expected: {4}",
				Environment.NewLine, errMsg, template, actual, expected);
		}

		[Test]
		public void TestCodeBlocksWithNestedScope()
		{
			RunTest(@"<?csharp var oranges = new List<int> { 1, 2, 3 }; ?><div>
<?csharp var bananas = new List<int> { 1, 2, 3 }; ?><ul>
<?csharp
foreach(var banana in bananas) output.Write(""<li>banana {0}</li>"", banana);
foreach(var orange in oranges) output.Write(""<li>orange {0}</li>"", orange);
?>
</ul>
</div>",
			   @"<div>
<ul>
<li>banana 1</li><li>banana 2</li><li>banana 3</li><li>orange 1</li><li>orange 2</li><li>orange 3</li>
</ul>
</div>",
			   "CodeBlocks with nested scope failed!");
		}
	}
}
