using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace SharpTAL.SharpTALTests.METALTests
{
	[TestFixture]
	public class METALDefineSlotsTests
	{
		public static SharpTAL.ITemplateCache cache;
		public static Dictionary<string, object> globals;

		[TestFixtureSetUp]
		public void SetUpClass()
		{
			// Using MemoryTemplateCache in this tests
			cache = new MemoryTemplateCache();
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
			globals.Add("link", "www.owlfish.com");
			globals.Add("needsQuoting", @"Does ""this"" & work?");
		}

		public static void RunTest(string macros, string template, string expected, string errMsg)
		{
			template += macros;
			string actual = cache.RenderTemplate(template, globals);
			actual = actual.Replace("{", "{{").Replace("}", "}}");
			Assert.AreEqual(expected, actual, "{1} - {0}template: {2}{0}actual: {3}{0}expected: {4}",
				Environment.NewLine, errMsg, template, actual, expected);
		}

		[Test]
		public void TestSingleSlot()
		{
			RunTest(@"<div metal:define-macro=""one"" class=""funny"">Before <b metal:define-slot=""blue"">blue</b> After</div>",
				@"<html><body metal:use-macro='macros[""one""]'>Nowt <i metal:fill-slot=""blue"">white</i> here</body></html>",
				@"<html><div class=""funny"">Before <i>white</i> After</div></html>",
				"Single slot expansion failed.");
		}

		[Test]
		public void TestDoubleSlot()
		{
			RunTest(@"<div metal:define-macro=""one"" class=""funny"">Before <b metal:define-slot=""blue"">blue</b> After <a metal:define-slot=""red"">red</a></div>",
				@"<html><body metal:use-macro='macros[""one""]'>Nowt <i metal:fill-slot=""blue"">white</i> here <b metal:fill-slot=""red"">black</b></body></html>",
				@"<html><div class=""funny"">Before <i>white</i> After <b>black</b></div></html>",
				"Double slot expansion failed.");
		}

		[Test]
		public void TestDoubleOneDefaultSlot()
		{
			RunTest(@"<div metal:define-macro=""one"" class=""funny"">Before <b metal:define-slot=""blue"">blue</b> After <a metal:define-slot=""red"">red</a></div>",
				@"<html><body metal:use-macro='macros[""one""]'>Nowt <i metal:fill-slot=""blue"">white</i> here <b metal:fill-slot=""purple"">purple</b></body></html>",
				@"<html><div class=""funny"">Before <i>white</i> After <a>red</a></div></html>",
				"Double slot with default, expansion failed.");
		}

		[Test]
		public void TestDoubleMacroDefaultSlot()
		{
			RunTest(@"<p metal:define-macro=""two"">Internal macro, colour blue: <b metal:define-slot=""blue"">blue</b></p><div metal:define-macro=""one"" class=""funny"">Before <b metal:define-slot=""blue"">blue</b> After <a metal:use-macro='macros[""two""]'>Internal</a></div>",
				@"<html><body metal:use-macro='macros[""one""]'>Nowt <i metal:fill-slot=""blue"">white</i></body></html>",
				@"<html><div class=""funny"">Before <i>white</i> After <p>Internal macro, colour blue: <b>blue</b></p></div></html>",
				"Nested macro with same slot name.");
		}

		[Test]
		public void TestDoubleMacroDoubleFillSlot()
		{
			RunTest(@"<p metal:define-macro=""two"">Internal macro, colour blue: <b metal:define-slot=""blue"">blue</b></p><div metal:define-macro=""one"" class=""funny"">Before <b metal:define-slot=""blue"">blue</b> After <a metal:use-macro='macros[""two""]'>Internal<p metal:fill-slot=""blue"">pink!</p></a> finally outer blue again: <a metal:define-slot=""blue"">blue goes here</a></div>",
				@"<html><body metal:use-macro='macros[""one""]'>Nowt <i metal:fill-slot=""blue"">white</i></body></html>",
				@"<html><div class=""funny"">Before <i>white</i> After <p>Internal macro, colour blue: <p>pink!</p></p> finally outer blue again: <i>white</i></div></html>",
				"Nested macro with same slot name and slot being used failed.");
		}

		[Test]
		public void TestSingleSlotDefaultTAL()
		{
			RunTest(@"<div metal:define-macro=""one"" class=""funny"">Before <b metal:define-slot=""blue"" tal:content=""test"">blue</b> After</div>",
				@"<html><body metal:use-macro='macros[""one""]'>Nowt here</body></html>",
				@"<html><div class=""funny"">Before <b>testing</b> After</div></html>",
				"Slot defaulting that holds TAL failed.");
		}

		[Test]
		public void TestSingleSlotPassedInTAL()
		{
			RunTest(@"<div metal:define-macro=""one"" class=""funny"">Before <b metal:define-slot=""blue"" tal:content=""test"">blue</b> After</div>",
				@"<html><body metal:use-macro='macros[""one""]'>Nowt <i metal:fill-slot=""blue"" tal:content=""needsQuoting"" tal:attributes=""href link"">boo</i> here</body></html>",
				@"<html><div class=""funny"">Before <i href=""www.owlfish.com"">Does ""this"" &amp; work?</i> After</div></html>",
				"Slot filled with TAL failed.");
		}
	}
}
