using System.IO;
using NUnit.Framework;
using SharpTAL.TemplateCache;

namespace SharpTAL.Tests.CacheTests
{
	[TestFixture]
	public class MemoryTemplateCacheTests : CacheTestsBase
	{
		[TestFixtureSetUp]
		public void SetUpClass()
		{
			Cache = new MemoryTemplateCache();
		}

		[Test]
		public void TestReuseTemplateInfo()
		{
			TemplateInfo ti = Cache.CompileTemplate(@"<template></template>", Globals, RefAssemblies);
			string hash1 = ti.TemplateKey;

			ti = Cache.CompileTemplate(@"<template></template>", Globals, RefAssemblies);
			string hash2 = ti.TemplateKey;
			Assert.AreEqual(hash1, hash2, "Reusing generated template in Cache failed. Hash1: {0}, Hash2: {1}", hash1, hash2);
		}

		[Test]
		public void TestTemplateChange()
		{
			TemplateInfo ti = Cache.CompileTemplate(@"<template></template>", Globals, RefAssemblies);
			string hash1 = ti.TemplateKey;

			ti = Cache.CompileTemplate(@"<template2></template2>", Globals, RefAssemblies);
			string hash2 = ti.TemplateKey;
			Assert.AreNotEqual(hash1, hash2, "Template re-generation if template change failed. Hash1: {0}, Hash2: {1}", hash1, hash2);
		}

		[Test]
		public void TestGlobalsChange()
		{
			TemplateInfo ti = Cache.CompileTemplate(@"<template></template>", Globals, RefAssemblies);
			string hash1 = ti.TemplateKey;

			Globals.Add("the_string_2", typeof(string));

			ti = Cache.CompileTemplate(@"<template></template>", Globals, RefAssemblies);
			string hash2 = ti.TemplateKey;
			Assert.AreNotEqual(hash1, hash2, "Template re-generation if Globals types change failed. Hash1: {0}, Hash2: {1}", hash1, hash2);
		}

		[Test]
		public void TestRefAssembliesChange()
		{
			TemplateInfo ti = Cache.CompileTemplate(@"<template></template>", Globals, RefAssemblies);
			string hash1 = ti.TemplateKey;

			RefAssemblies.Clear();

			ti = Cache.CompileTemplate(@"<template></template>", Globals, RefAssemblies);
			string hash2 = ti.TemplateKey;
			Assert.AreNotEqual(hash1, hash2, "Template re-generation if referenced assemblies change failed. Hash1: {0}, Hash2: {1}", hash1, hash2);
		}

		[Test]
		public void TestImportChange()
		{
			const string templateBody = @"<template metal:import='TestImportChange_Imports.xml'></template>";

			File.WriteAllText("TestImportChange_Imports.xml", "<import1></import1>");

			TemplateInfo ti = Cache.CompileTemplate(templateBody, Globals, RefAssemblies);
			string hash1 = ti.TemplateKey;

			File.WriteAllText("TestImportChange_Imports.xml", "<import2></import2>");

			ti = Cache.CompileTemplate(templateBody, Globals, RefAssemblies);
			string hash2 = ti.TemplateKey;
			Assert.AreNotEqual(hash1, hash2, "Template re-generation if referenced assemblies change failed. Hash1: {0}, Hash2: {1}", hash1, hash2);
		}
	}
}
