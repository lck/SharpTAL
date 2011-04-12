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
	public abstract class AbstractTemplateCache : ITemplateCache
	{
		/// <summary>
		/// Render the template
		/// </summary>
		/// <param name="output">The output stream</param>
		/// <param name="templateBody">The template body</param>
		public void RenderTemplate(StreamWriter output, string templateBody)
		{
			RenderTemplate(output, templateBody, null, null, null);
		}

		/// <summary>
		/// Render the template
		/// </summary>
		/// <param name="output">The output stream</param>
		/// <param name="templateBody">The template body</param>
		/// <param name="globals">Dictionary of global variables</param>
		public void RenderTemplate(StreamWriter output, string templateBody, Dictionary<string, object> globals)
		{
			RenderTemplate(output, templateBody, globals, null, null);
		}

		/// <summary>
		/// Render the template
		/// </summary>
		/// <param name="output">The output stream</param>
		/// <param name="templateBody">The template body</param>
		/// <param name="globals">Dictionary of global variables</param>
		/// <param name="inlineTemplates">Dictionary of inline templates</param>
		public void RenderTemplate(StreamWriter output, string templateBody, Dictionary<string, object> globals,
			Dictionary<string, string> inlineTemplates)
		{
			RenderTemplate(output, templateBody, globals, inlineTemplates, null);
		}

		/// <summary>
		/// Render the template
		/// </summary>
		/// <param name="output">The output stream</param>
		/// <param name="templateBody">The template body</param>
		/// <param name="globals">Dictionary of global variables</param>
		/// <param name="inlineTemplates">Dictionary of inline templates</param>
		/// <param name="referencedAssemblies">List of referenced assemblies</param>
		public void RenderTemplate(StreamWriter output, string templateBody, Dictionary<string, object> globals,
			Dictionary<string, string> inlineTemplates,
			List<Assembly> referencedAssemblies)
		{
			TemplateInfo ti;
			RenderTemplate(output, templateBody, globals, inlineTemplates, referencedAssemblies, out ti);
		}

		/// <summary>
		/// Render the template
		/// </summary>
		/// <param name="output">The output stream</param>
		/// <param name="templateBody">The template body</param>
		/// <param name="globals">Dictionary of global variables</param>
		/// <param name="inlineTemplates">Dictionary of inline templates</param>
		/// <param name="referencedAssemblies">List of referenced assemblies</param>
		/// <param name="sourceCode">Template source code</param>
		public void RenderTemplate(StreamWriter output, string templateBody, Dictionary<string, object> globals,
			Dictionary<string, string> inlineTemplates, List<Assembly> referencedAssemblies, out TemplateInfo templateInfo)
		{
			templateInfo = null;

			if (string.IsNullOrEmpty(templateBody))
			{
				return;
			}

			// Get template info from cache
			templateInfo = GetTemplateInfo(templateBody, globals, inlineTemplates, referencedAssemblies);

			// Call the Render() method
			try
			{
				templateInfo.TemplateRenderMethod.Invoke(null, new object[] { output, globals });
			}
			catch (TargetInvocationException ex)
			{
				throw new RenderTemplateException(templateInfo, ex.InnerException.Message, ex.InnerException);
			}
			catch (Exception ex)
			{
				throw new RenderTemplateException(templateInfo, ex.Message, ex);
			}
		}

		/// <summary>
		/// Render the template
		/// </summary>
		/// <param name="templateBody">The template body</param>
		/// <returns>Rendered template</returns>
		public string RenderTemplate(string templateBody)
		{
			return RenderTemplate(templateBody, null, null, null);
		}

		/// <summary>
		/// Render the template
		/// </summary>
		/// <param name="templateBody">The template body</param>
		/// <param name="globals">Dictionary of global variables</param>
		/// <returns>Rendered template</returns>
		public string RenderTemplate(string templateBody, Dictionary<string, object> globals)
		{
			return RenderTemplate(templateBody, globals, null, null);
		}

		/// <summary>
		/// Render the template
		/// </summary>
		/// <param name="templateBody">The template body</param>
		/// <param name="globals">Dictionary of global variables</param>
		/// <param name="inlineTemplates">Dictionary of inline templates</param>
		/// <returns>Rendered template</returns>
		public string RenderTemplate(string templateBody, Dictionary<string, object> globals,
			Dictionary<string, string> inlineTemplates)
		{
			return RenderTemplate(templateBody, globals, inlineTemplates, null);
		}

		/// <summary>
		/// Render the template
		/// </summary>
		/// <param name="templateBody">The template body</param>
		/// <param name="globals">Dictionary of global variables</param>
		/// <param name="inlineTemplates">Dictionary of inline templates</param>
		/// <param name="referencedAssemblies">List of referenced assemblies</param>
		/// <returns>Rendered template</returns>
		public string RenderTemplate(string templateBody, Dictionary<string, object> globals,
			Dictionary<string, string> inlineTemplates,
			List<Assembly> referencedAssemblies)
		{
			TemplateInfo ti;
			return RenderTemplate(templateBody, globals, inlineTemplates, referencedAssemblies, out ti);
		}

		/// <summary>
		/// Render the template
		/// </summary>
		/// <param name="templateBody">The template body</param>
		/// <param name="globals">Dictionary of global variables</param>
		/// <param name="inlineTemplates">Dictionary of inline templates</param>
		/// <param name="referencedAssemblies">List of referenced assemblies</param>
		/// <param name="sourceCode">Template source code</param>
		/// <returns>Rendered template</returns>
		public string RenderTemplate(string templateBody, Dictionary<string, object> globals,
			Dictionary<string, string> inlineTemplates, List<Assembly> referencedAssemblies, out TemplateInfo templateInfo)
		{
			// Expand template
			MemoryStream stream = new MemoryStream();
			StreamWriter writer = new StreamWriter(stream);
			RenderTemplate(writer, templateBody, globals, inlineTemplates, referencedAssemblies, out templateInfo);
			writer.Flush();

			// Reset stream position
			stream.Position = 0;

			// Read stream to string
			StreamReader reader = new StreamReader(stream);
			string result = reader.ReadToEnd();

			writer.Close();

			return result;
		}

		/// <summary>
		/// Get template info from cache.
		/// </summary>
		/// <param name="templateBody">The template body</param>
		/// <param name="globals">Dictionary of global variables</param>
		/// <param name="inlineTemplates">Dictionary of inline templates</param>
		/// <param name="referencedAssemblies">List of referenced assemblies</param>
		/// <param name="sourceCode">Template source code</param>
		/// <returns>The TemplateInfo instance</returns>
		protected abstract TemplateInfo GetTemplateInfo(string templateBody, Dictionary<string, object> globals,
			Dictionary<string, string> inlineTemplates, List<Assembly> referencedAssemblies);

		protected MethodInfo GetTemplateRenderMethod(Assembly assembly, TemplateInfo ti)
		{
			string templateTypeFullName = string.Format("Templates.Template_{0}", ti.TemplateHash);

			// Check if assembly contains the template type
			Type templateType = assembly.GetType(templateTypeFullName);
			if (templateType == null)
			{
				throw new Exception(string.Format("Failed to find type [{0}] in assembly [{1}].",
					templateTypeFullName, assembly.FullName));
			}

			// Check if the template type has public method [static void Render(StreamWriter output, Dictionary<string, object>)]
			MethodInfo renderMethod = templateType.GetMethod("Render",
				BindingFlags.Public | BindingFlags.Static,
				null, new Type[] { typeof(StreamWriter), typeof(Dictionary<string, object>) }, null);

			if (renderMethod == null || renderMethod.ReturnType.FullName != "System.Void")
			{
				throw new Exception(string.Format(@"Failed to find Render method in type [{0}] in assembly [{1}].
The signature of method must be [static void Render(StreamWriter output, Dictionary<string, object>)]",
					templateTypeFullName, assembly.FullName));
			}

			return renderMethod;
		}
	}
}
