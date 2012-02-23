namespace SharpTAL.SharpTALTests.CacheTests
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
	public class FileSystemTemplateCacheTests
	{
		public static FileSystemTemplateCache cache;
		public static Dictionary<string, Type> globals;
		public static List<Assembly> refAssemblies;

		[TestFixtureSetUp]
		public void SetUpClass()
		{
			string cacheFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Template Cache");
			if (!Directory.Exists(cacheFolder))
			{
				Directory.CreateDirectory(cacheFolder);
			}
			cache = new FileSystemTemplateCache(cacheFolder, true, typeof(FileSystemTemplateCacheTests).Name + "_{key}.dll");
		}

		[TestFixtureTearDown]
		public void CleanupClass()
		{
		}

		[SetUp]
		public void SetUp()
		{
			globals = new Dictionary<string, Type>();
			globals.Add("the_string", typeof(string));

			refAssemblies = new List<Assembly>();
			refAssemblies.Add(typeof(MemoryTemplateCacheTests).Assembly);
		}

		[Test]
		public void TestReuseTemplateInfo()
		{
			TemplateInfo ti;

			ti = cache.CompileTemplate(@"<template></template>", globals, refAssemblies);
			string hash1 = ti.TemplateKey;

			ti = cache.CompileTemplate(@"<template></template>", globals, refAssemblies);
			string hash2 = ti.TemplateKey;
			Assert.AreEqual(hash1, hash2, "Reusing generated template in cache failed. Hash1: {0}, Hash2: {1}", hash1, hash2);
		}

		[Test]
		public void TestTemplateChange()
		{
			TemplateInfo ti;

			ti = cache.CompileTemplate(@"<template></template>", globals, refAssemblies);
			string hash1 = ti.TemplateKey;

			ti = cache.CompileTemplate(@"<template2></template2>", globals, refAssemblies);
			string hash2 = ti.TemplateKey;
			Assert.AreNotEqual(hash1, hash2, "Template re-generation if template change failed. Hash1: {0}, Hash2: {1}", hash1, hash2);
		}

		[Test]
		public void TestGlobalsChange()
		{
			TemplateInfo ti;

			ti = cache.CompileTemplate(@"<template></template>", globals, refAssemblies);
			string hash1 = ti.TemplateKey;

			globals.Add("the_string_2", typeof(string));

			ti = cache.CompileTemplate(@"<template></template>", globals, refAssemblies);
			string hash2 = ti.TemplateKey;
			Assert.AreNotEqual(hash1, hash2, "Template re-generation if globals types change failed. Hash1: {0}, Hash2: {1}", hash1, hash2);
		}

		[Test]
		public void TestRefAssembliesChange()
		{
			TemplateInfo ti;

			ti = cache.CompileTemplate(@"<template></template>", globals, refAssemblies);
			string hash1 = ti.TemplateKey;

			refAssemblies.Clear();

			ti = cache.CompileTemplate(@"<template></template>", globals, refAssemblies);
			string hash2 = ti.TemplateKey;
			Assert.AreNotEqual(hash1, hash2, "Template re-generation if referenced assemblies change failed. Hash1: {0}, Hash2: {1}", hash1, hash2);
		}

		[Test]
		public void TestImportChange()
		{
			TemplateInfo ti;
			string templateBody = @"<template metal:import='TestImportChange_Imports.xml'></template>";

			File.WriteAllText("TestImportChange_Imports.xml", "<import1></import1>");

			ti = cache.CompileTemplate(templateBody, globals, refAssemblies);
			string hash1 = ti.TemplateKey;

			File.WriteAllText("TestImportChange_Imports.xml", "<import2></import2>");

			ti = cache.CompileTemplate(templateBody, globals, refAssemblies);
			string hash2 = ti.TemplateKey;
			Assert.AreNotEqual(hash1, hash2, "Template re-generation if referenced assemblies change failed. Hash1: {0}, Hash2: {1}", hash1, hash2);
		}
	}
}
