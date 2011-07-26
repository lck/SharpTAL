//
// AbstractTemplateCache.cs
//
// Author:
//   Roman Lacko (backup.rlacko@gmail.com)
//
// Copyright (c) 2010 - 2011 Roman Lacko
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
using System.IO;
using System.Reflection;
using System.Globalization;

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
            RenderTemplate(output, templateBody, globals, inlineTemplates, referencedAssemblies, out templateInfo, CultureInfo.InvariantCulture);
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
        /// <param name="culture">Culture to use for string conversions. Default is invariant culture.</param>
        public void RenderTemplate(StreamWriter output, string templateBody, Dictionary<string, object> globals,
            Dictionary<string, string> inlineTemplates, List<Assembly> referencedAssemblies, out TemplateInfo templateInfo,
            CultureInfo culture)
        {
            templateInfo = null;

            if (string.IsNullOrEmpty(templateBody))
            {
                return;
            }

            Dictionary<string, Type> globalsTypes = new Dictionary<string, Type>();
            if (globals != null)
            {
                foreach (string objName in globals.Keys)
                {
                    object obj = globals[objName];
                    globalsTypes.Add(objName, obj != null ? obj.GetType() : null);
                }
            }

            // Get template info from cache
            templateInfo = GetTemplateInfo(templateBody, globalsTypes, inlineTemplates, referencedAssemblies);

            // Call the Render() method
            try
            {
                templateInfo.TemplateRenderMethod.Invoke(null, new object[] { output, globals, culture });
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
        /// <param name="culture">Culture to use for string conversions. Default is invariant culture.</param>
        /// <returns>Rendered template</returns>
        public string RenderTemplate(string templateBody, Dictionary<string, object> globals,
            Dictionary<string, string> inlineTemplates, List<Assembly> referencedAssemblies, out TemplateInfo templateInfo)
        {
            return RenderTemplate(templateBody, globals, inlineTemplates, referencedAssemblies, out templateInfo, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Render the template
        /// </summary>
        /// <param name="templateBody">The template body</param>
        /// <param name="globals">Dictionary of global variables</param>
        /// <param name="inlineTemplates">Dictionary of inline templates</param>
        /// <param name="referencedAssemblies">List of referenced assemblies</param>
        /// <param name="sourceCode">Template source code</param>
        /// 
        /// <returns>Rendered template</returns>
        public string RenderTemplate(string templateBody, Dictionary<string, object> globals,
            Dictionary<string, string> inlineTemplates, List<Assembly> referencedAssemblies, out TemplateInfo templateInfo,
            CultureInfo culture)
        {
            // Expand template
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            RenderTemplate(writer, templateBody, globals, inlineTemplates, referencedAssemblies, out templateInfo, culture);
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
        /// Precompile template to ensure that the compiled assembly is already in cache when
        /// RenderTemplate is called for the first time. For precompiling, the actual values
        /// of globals are not required, just the names and types of the global variables.
        /// </summary>
        /// <param name="templateBody">The template body</param>
        /// <param name="globalsTypes">Dictionary of global variable names and types, or null for no global variables.</param>
        /// <param name="inlineTemplates">Dictionary of inline templates, or null for no inline templates.</param>
        /// <param name="referencedAssemblies">List of referenced assemblies, or null for no referenced assemblies.</param>
        public void PrecompileTemplate(string templateBody, Dictionary<string, Type> globalsTypes,
            Dictionary<string, string> inlineTemplates, List<Assembly> referencedAssemblies)
        {
            GetTemplateInfo(templateBody, globalsTypes, inlineTemplates, referencedAssemblies);
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
        protected abstract TemplateInfo GetTemplateInfo(string templateBody, Dictionary<string, Type> globalsTypes,
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
                null, new Type[] { typeof(StreamWriter), typeof(Dictionary<string, object>), typeof(CultureInfo) }, null);

            if (renderMethod == null || renderMethod.ReturnType.FullName != "System.Void")
            {
                throw new Exception(string.Format(@"Failed to find Render method in type [{0}] in assembly [{1}].
The signature of method must be [static void Render(StreamWriter output, Dictionary<string, object>, CultureInfo culture)]",
                    templateTypeFullName, assembly.FullName));
            }

            return renderMethod;
        }
    }
}
