//
// Errors.cs
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
	using System.CodeDom.Compiler;
	using SharpTAL.TemplateParser;
	
	public class TemplateParseException : Exception
    {
        protected Tag m_Tag;
        protected string m_ErrorDescription;

        public TemplateParseException(Tag tag, string errorDescription)
            : base(errorDescription)
        {
            this.m_Tag = tag;
            this.m_ErrorDescription = errorDescription;
        }

        public override string Message
        {
            get
            {
                if (this.m_Tag != null)
                {
                    return string.Format("{1}{0}Tag: {2}{0}Source: {3}{0}Line: {4}{0}Position: {5}",
                        Environment.NewLine, this.m_ErrorDescription,
                        this.m_Tag.ToString(), this.m_Tag.SourcePath, this.m_Tag.LineNumber, this.m_Tag.LinePosition);
                }
                return this.m_ErrorDescription;
            }
        }

        public override string ToString()
        {
            return this.Message;
        }
    }

    public class CompileSourceException : Exception
    {
        protected TemplateInfo m_TemplateInfo;
        protected CompilerErrorCollection m_Errors;

        public CompileSourceException(TemplateInfo templateInfo, CompilerErrorCollection errors, string message)
            : base(message)
        {
            m_TemplateInfo = templateInfo;
        }

        public TemplateInfo TemplateInfo
        {
            get
            {
                return this.m_TemplateInfo;
            }
        }

        public CompilerErrorCollection Errors
        {
            get
            {
                return this.m_Errors;
            }
        }
    }

    public class RenderTemplateException : Exception
    {
        protected TemplateInfo m_TemplateInfo;

        public RenderTemplateException(TemplateInfo templateInfo, string message, Exception innerException)
            : base(message, innerException)
        {
            m_TemplateInfo = templateInfo;
        }

        public TemplateInfo TemplateInfo
        {
            get
            {
                return this.m_TemplateInfo;
            }
        }
    }
}
