using System;
using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;

using SharpTAL.TemplateCache;

namespace SharpTAL.Tests.CacheTests
{
	public class CacheTestsBase
	{
		protected ITemplateCache Cache;
		protected Dictionary<string, Type> Globals;
		protected List<Assembly> RefAssemblies;

		[SetUp]
		public void SetUp()
		{
			Globals = new Dictionary<string, Type>
			{
				{ "the_string", typeof(string) }
			};

			RefAssemblies = new List<Assembly>
			{
				GetType().Assembly
			};
		}
	}
}
