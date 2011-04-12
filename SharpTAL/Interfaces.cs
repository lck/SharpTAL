using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace SharpTAL.Interfaces
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

		string RenderTemplate(string templateBody);

		string RenderTemplate(string templateBody, Dictionary<string, object> globals);

		string RenderTemplate(string templateBody, Dictionary<string, object> globals,
			Dictionary<string, string> inlineTemplates);

		string RenderTemplate(string templateBody, Dictionary<string, object> globals,
			Dictionary<string, string> inlineTemplates, List<Assembly> referencedAssemblies);

		string RenderTemplate(string templateBody, Dictionary<string, object> globals,
			Dictionary<string, string> inlineTemplates, List<Assembly> referencedAssemblies, out TemplateInfo templateInfo);
	}
}
