//
// ITemplateCache.cs
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

using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Globalization;

namespace SharpTAL
{
    public interface ITemplateCache
    {
        void RenderTemplate(StreamWriter output, string templateBody);

        void RenderTemplate(StreamWriter output, string templateBody, Dictionary<string, object> globals);

        void RenderTemplate(StreamWriter output, string templateBody, Dictionary<string, object> globals,
            Dictionary<string, string> inlineTemplates);

        void RenderTemplate(StreamWriter output, string templateBody, Dictionary<string, object> globals,
            Dictionary<string, string> inlineTemplates, List<Assembly> referencedAssemblies);

        void RenderTemplate(StreamWriter output, string templateBody, Dictionary<string, object> globals,
            Dictionary<string, string> inlineTemplates, List<Assembly> referencedAssemblies, out TemplateInfo templateInfo);

        void RenderTemplate(StreamWriter output, string templateBody, Dictionary<string, object> globals,
            Dictionary<string, string> inlineTemplates, List<Assembly> referencedAssemblies, out TemplateInfo templateInfo,
            CultureInfo culture);

        string RenderTemplate(string templateBody);

        string RenderTemplate(string templateBody, Dictionary<string, object> globals);

        string RenderTemplate(string templateBody, Dictionary<string, object> globals,
            Dictionary<string, string> inlineTemplates);

        string RenderTemplate(string templateBody, Dictionary<string, object> globals,
            Dictionary<string, string> inlineTemplates, List<Assembly> referencedAssemblies);

        string RenderTemplate(string templateBody, Dictionary<string, object> globals,
            Dictionary<string, string> inlineTemplates, List<Assembly> referencedAssemblies, out TemplateInfo templateInfo);

        string RenderTemplate(string templateBody, Dictionary<string, object> globals,
            Dictionary<string, string> inlineTemplates, List<Assembly> referencedAssemblies, out TemplateInfo templateInfo,
            CultureInfo culture);
    }
}
