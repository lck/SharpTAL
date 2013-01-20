//
// TemplateInfo.cs
//
// Author:
//   Roman Lacko (backup.rlacko@gmail.com)
//
// Copyright (c) 2010 - 2013 Roman Lacko
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

namespace SharpTAL
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.IO;
	using System.Globalization;
	using SharpTAL.TemplateProgram;

	public class TemplateInfo
	{
		#region Template Cache output
		
		/// <summary>
		/// Contains a unique key that represent the template in the template cache.
		/// The template key is computed from the following parts:
		///	 Template body hash
		///	 Global types hash
		///	 Imported templates hash
		///	 Referenced assemblies hash
		/// </summary>
		public string TemplateKey;

		// TODO: sem dat zoznam render() metod (aj tych z makier), ale v akom tvare ? ako MethodInfo ?
		//	maly by byt v takom tvare aby sa dali priamo volat z inej template render() metody ktora tento template importuje
		//	to znamena ze metal:import bude importovat assemblies, nebude injectovat importovane templates do main template
		/// <summary>
		/// Reference to template render method.
		/// </summary>
		public MethodInfo RenderMethod;

		/// <summary>
		/// Template generator version.
		/// </summary>
		public string GeneratorVersion;

		#endregion

		#region Template Parser input

		/// <summary>
		/// Template body.
		/// </summary>
		public string TemplateBody;

		#endregion

		#region Code Generator input

		/// <summary>
		/// Types of global objects
		/// The key contains the object name.
		/// The value contains the object type.
		/// </summary>
		public Dictionary<string, Type> GlobalsTypes;

		/// <summary>
		/// List of referenced assemblies.
		/// </summary>
		public List<Assembly> ReferencedAssemblies;

		#region Template Parser output

		/// <summary>
		/// Main template program compiled from TemplateBody.
		/// </summary>
		public Program MainProgram;

		/// <summary>
		/// Dictionary of TemplatePrograms compiled from templates imported by "metal:import" command.
		/// The key contains the full path to the template file.
		/// The value contains the compiled program.
		/// </summary>
		public Dictionary<string, Program> ImportedPrograms;

		/// <summary>
		/// Dictionary of paths to imported programs hashed by namespace created by "metal:import" command.
		/// The key contains the imported program namespace.
		/// The value contains the list of full paths to template files.
		/// </summary>
		public Dictionary<string, HashSet<string>> ImportedNamespaces;

		#endregion

		#endregion

		#region Code Generator output

		/// <summary>
		/// Contains the C# source code used to compile the assembly.
		/// </summary>
		public string GeneratedSourceCode;

		#endregion
	}
}
