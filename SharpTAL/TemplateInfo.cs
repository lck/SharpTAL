using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.CodeDom.Compiler;
using System.Security.Cryptography;

using Microsoft.CSharp;

using SharpTAL.Interfaces;

namespace SharpTAL
{
	public class TemplateInfo
	{
		/// <summary>
		/// Source xml of the template.
		/// </summary>
		public string TemplateBody;

		/// <summary>
		/// Types of global objects
		/// The key contains the object name.
		/// The value contains the object type.
		/// </summary>
		public Dictionary<string, Type> GlobalsTypes;

		/// <summary>
		/// Dictionary of inline templates xml sources.
		/// The key contains the inline template name.
		/// The value contains the inline template source xml.
		/// </summary>
		public Dictionary<string, string> InlineTemplates;

		/// <summary>
		/// List of referenced assemblies.
		/// </summary>
		public List<Assembly> ReferencedAssemblies;

		/// <summary>
		/// Reference to generated method used to render the template.
		/// </summary>
		public MethodInfo TemplateRenderMethod;

		/// <summary>
		/// Contains a unique key that represent the template in the template cache.
		/// It is computed from:
		///		"Template Body"
		///		"Full Names of Global Types"
		///		"Inline Templates Bodies"
		///		"Imported Templates Bodies"
		///		"Full Names of Referenced Assemblies"
		/// </summary>
		public string TemplateHash;

		/// <summary>
		/// Dictionary of TALPrograms compiled from TemplateBody and InlineTemplates.
		/// The key contains the template name.
		/// The value contains the compiled template program.
		/// </summary>
		public Dictionary<string, TALProgram> Programs;

		/// <summary>
		/// Dictionary of TALPrograms compiled from templates imported by "metal:import" command.
		/// The key contains the full path to the template source file.
		/// The value contains the compiled template program.
		/// </summary>
		public Dictionary<string, TALProgram> ImportedPrograms;

		/// <summary>
		/// Dictionary of template namespaces created by "metal:import" command.
		/// The key contains the imported template namespace.
		/// The value contains the list of full paths to template source files.
		/// </summary>
		public Dictionary<string, HashSet<string>> ImportedNamespaces;

		/// <summary>
		/// Contains the C# source code used to compile the assembly.
		/// </summary>
		public string GeneratedSourceCode;
	}
}
