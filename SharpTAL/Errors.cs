using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Xml;

namespace SharpTAL
{
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
		
		public CompileSourceException(TemplateInfo templateInfo, string message)
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
