//
// MemoryTemplateCache.cs
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
using System.Reflection;

namespace SharpTAL
{
    public class MemoryTemplateCache : AbstractTemplateCache
    {
        object m_CacheLock;
        Dictionary<string, TemplateInfo> m_Cache;

        /// <summary>
        /// Initialize the template cache
        /// </summary>
        public MemoryTemplateCache()
        {
            m_CacheLock = new object();
            m_Cache = new Dictionary<string, TemplateInfo>();
        }

        protected override TemplateInfo GetTemplateInfo(string templateBody, Dictionary<string, Type> globalsTypes,
            Dictionary<string, string> inlineTemplates, List<Assembly> referencedAssemblies)
        {
            lock (m_CacheLock)
            {
                // Create template info
                TemplateInfo ti = new TemplateInfo()
                {
                    TemplateBody = templateBody,
                    TemplateHash = null,
                    GlobalsTypes = globalsTypes,
                    ReferencedAssemblies = referencedAssemblies,
                    InlineTemplates = inlineTemplates,
                    TemplateRenderMethod = null
                };

                // Compile Template to TALPrograms and generate the TemplateHash
                TALCompiler.CompileTemplate(ti);

                // Generated template found in cache
                if (m_Cache.ContainsKey(ti.TemplateHash))
                {
                    return m_Cache[ti.TemplateHash];
                }

                // Generate source
                SourceGenerator sourceGenerator = new SourceGenerator();
                ti.GeneratedSourceCode = sourceGenerator.GenerateSource(ti);

                // Generate assembly
                AssemblyGenerator assemblyCompiler = new AssemblyGenerator();
                Assembly assembly = assemblyCompiler.GenerateAssembly(ti, true, null, null);

                // Try to load the Render() method from assembly
                ti.TemplateRenderMethod = GetTemplateRenderMethod(assembly, ti);

                m_Cache.Add(ti.TemplateHash, ti);

                return ti;
            }
        }
    }
}
