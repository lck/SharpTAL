//
// ITemplateCache.cs
//
// Author:
//   Roman Lacko (backup.rlacko@gmail.com)
//
// Copyright (c) 2010 - 2012 Roman Lacko
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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Globalization;

namespace SharpTAL.TemplateCache
{
	public interface ITemplateCache
	{
		/// <summary>
		/// Compile template to ensure that the compiled assembly is already in cache when
		/// RenderTemplate is called for the first time. For precompiling, the actual values
		/// of globals are not required, just the names and types of the global variables.
		/// </summary>
		/// <param name="templateBody">The template body</param>
		/// <param name="globals">Dictionary of global variables</param>
		/// <param name="referencedAssemblies">List of referenced assemblies</param>
		/// <returns>The TemplateInfo generated from compiled template body</returns>
		TemplateInfo CompileTemplate(string templateBody, Dictionary<string, Type> globalsTypes, List<Assembly> referencedAssemblies);

		/// <summary>
		/// Render the template
		/// </summary>
		/// <param name="output">The output stream</param>
		/// <param name="templateBody">The template body</param>
		void RenderTemplate(StreamWriter output, string templateBody);

		/// <summary>
		/// Render the template
		/// </summary>
		/// <param name="output">The output stream</param>
		/// <param name="templateBody">The template body</param>
		/// <param name="globals">Dictionary of global variables</param>
		void RenderTemplate(StreamWriter output, string templateBody, Dictionary<string, object> globals);

		/// <summary>
		/// Render the template
		/// </summary>
		/// <param name="output">The output stream</param>
		/// <param name="templateBody">The template body</param>
		/// <param name="globals">Dictionary of global variables</param>
		/// <param name="referencedAssemblies">List of referenced assemblies</param>
		void RenderTemplate(StreamWriter output, string templateBody, Dictionary<string, object> globals, List<Assembly> referencedAssemblies);

		/// <summary>
		/// Render the template
		/// </summary>
		/// <param name="output">The output stream</param>
		/// <param name="templateBody">The template body</param>
		/// <param name="globals">Dictionary of global variables</param>
		/// <param name="referencedAssemblies">List of referenced assemblies</param>
		/// <param name="sourceCode">Template source code</param>
		void RenderTemplate(StreamWriter output, string templateBody, Dictionary<string, object> globals, List<Assembly> referencedAssemblies, out TemplateInfo templateInfo);

		/// <summary>
		/// Render the template
		/// </summary>
		/// <param name="output">The output stream</param>
		/// <param name="templateBody">The template body</param>
		/// <param name="globals">Dictionary of global variables</param>
		/// <param name="referencedAssemblies">List of referenced assemblies</param>
		/// <param name="sourceCode">Template source code</param>
		/// <param name="culture">Culture to use for string conversions. Default is invariant culture.</param>
		void RenderTemplate(StreamWriter output, string templateBody, Dictionary<string, object> globals, List<Assembly> referencedAssemblies, out TemplateInfo templateInfo, CultureInfo culture);

		/// <summary>
		/// Render the template
		/// </summary>
		/// <param name="templateBody">The template body</param>
		/// <returns>Rendered template</returns>
		string RenderTemplate(string templateBody);

		/// <summary>
		/// Render the template
		/// </summary>
		/// <param name="templateBody">The template body</param>
		/// <param name="globals">Dictionary of global variables</param>
		/// <returns>Rendered template</returns>
		string RenderTemplate(string templateBody, Dictionary<string, object> globals);

		/// <summary>
		/// Render the template
		/// </summary>
		/// <param name="templateBody">The template body</param>
		/// <param name="globals">Dictionary of global variables</param>
		/// <param name="referencedAssemblies">List of referenced assemblies</param>
		/// <returns>Rendered template</returns>
		string RenderTemplate(string templateBody, Dictionary<string, object> globals, List<Assembly> referencedAssemblies);

		/// <summary>
		/// Render the template
		/// </summary>
		/// <param name="templateBody">The template body</param>
		/// <param name="globals">Dictionary of global variables</param>
		/// <param name="referencedAssemblies">List of referenced assemblies</param>
		/// <param name="sourceCode">Template source code</param>
		/// <param name="culture">Culture to use for string conversions. Default is invariant culture.</param>
		/// <returns>Rendered template</returns>
		string RenderTemplate(string templateBody, Dictionary<string, object> globals, List<Assembly> referencedAssemblies, out TemplateInfo templateInfo);

		/// <summary>
		/// Render the template
		/// </summary>
		/// <param name="templateBody">The template body</param>
		/// <param name="globals">Dictionary of global variables</param>
		/// <param name="referencedAssemblies">List of referenced assemblies</param>
		/// <param name="sourceCode">Template source code</param>
		/// <returns>Rendered template</returns>
		string RenderTemplate(string templateBody, Dictionary<string, object> globals, List<Assembly> referencedAssemblies, out TemplateInfo templateInfo, CultureInfo culture);
	}
}
