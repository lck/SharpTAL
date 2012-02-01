//
// CodeGenerator.cs
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

namespace SharpTAL
{
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using System.IO;
	using System.Reflection;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Diagnostics;
	using ICSharpCode.NRefactory;
	using SharpTAL.TemplateParser;
	using SharpTAL.TemplateProgram;
	using SharpTAL.TemplateProgram.Commands;

	public class CodeGenerator : AbstractProgramInterpreter, ICodeGenerator
	{
		protected static readonly string FileBodyTemplate =
			@"${usings}

namespace Templates
{
    [SecurityPermission(SecurityAction.PermitOnly, Execution = true)]
    public class Template_${template_hash}
    {
        // TODO: public static void Render(StreamWriter output, Dictionary<string, object> globals, Func<string, object> formatResult)
        public static void Render(StreamWriter output, Dictionary<string, object> globals, CultureInfo culture)
        {
            Stack<List<object>> __programStack = new Stack<List<object>>();
            Stack<List<object>> __scopeStack = new Stack<List<object>>();
            bool __moveToEndTag = false;
            int __outputTag = 1;
            object __tagContent = null;
            int __tagContentType = 1;
            Dictionary<string, string> __macros = new Dictionary<string, string>();
            Dictionary<string, Attr> __currentAttributes = new Dictionary<string, Attr>();
            Dictionary<string, Attr> __repeatAttributesCopy = new Dictionary<string, Attr>();
            Dictionary<string, MacroDelegate> __slotMap = new Dictionary<string, MacroDelegate>();
            Dictionary<string, MacroDelegate> __currentSlots = new Dictionary<string, MacroDelegate>();
            Dictionary<string, object> __paramMap = new Dictionary<string, object>();
            Dictionary<string, object> __currentParams = new Dictionary<string, object>();
            CommandInfo __currentCmdInfo = new CommandInfo();
            
            // Template globals
            Dictionary<string, RepeatVariable> repeat = new Dictionary<string, RepeatVariable>();
            Dictionary<string, MacroDelegate> macros = new Dictionary<string, MacroDelegate>();
            
            try
            {
                // Globals
                ${globals}
                
                // Delegates
                MacroDelegate __CleanProgram = delegate()
                {
                    // Clean state
                    __scopeStack = new Stack<List<object>>();
                    __moveToEndTag = false;
                    __outputTag = 1;
                    __tagContent = null;
                    __tagContentType = 0;
                    __currentAttributes = new Dictionary<string, Attr>();
                    __currentCmdInfo = null;
                    // Used in repeat only.
                    __repeatAttributesCopy = new Dictionary<string, Attr>();
                    // Pass in the macro slots and params
                    __currentSlots = __slotMap;
                    __currentParams = __paramMap;
                };
                MacroDelegate __PushProgram = delegate()
                {
                    List<object> vars = new List<object>() {
                        __scopeStack,
                        __slotMap,
                        __paramMap,
                        __currentSlots,
                        __moveToEndTag,
                        __outputTag,
                        __tagContent,
                        __tagContentType,
                        __currentAttributes,
                        __repeatAttributesCopy,
                        __currentCmdInfo
                    };
                    __programStack.Push(vars);
                };
                MacroDelegate __PopProgram = delegate()
                {
                    List<object> vars = __programStack.Pop();
                    __scopeStack = (Stack<List<object>>)vars[0];
                    __slotMap = (Dictionary<string, MacroDelegate>)vars[1];
                    __paramMap = (Dictionary<string, object>)vars[2];
                    __currentSlots = (Dictionary<string, MacroDelegate>)vars[3];
                    __moveToEndTag = (bool)vars[4];
                    __outputTag = (int)vars[5];
                    __tagContent = (object)vars[6];
                    __tagContentType = (int)vars[7];
                    __currentAttributes = (Dictionary<string, Attr>)vars[8];
                    __repeatAttributesCopy = (Dictionary<string, Attr>)vars[9];
                    __currentCmdInfo = (CommandInfo)vars[10];
                };
                
                ${body}
            }
            catch (Exception ex)
            {
                string msg = string.Format(""Render method failed with following error:{0}  {1}"", Environment.NewLine, ex.Message);
                
                // Current Command Info
                msg = string.Format(""{0}{1}{1}Current Command Info:"", msg, Environment.NewLine);
                if (__currentCmdInfo != null)
                {
                    msg = string.Format(@""{0}{1}  Command:    {2}"", msg, Environment.NewLine, __currentCmdInfo.CommandName);
                    msg = string.Format(@""{0}{1}  Tag:        {2}"", msg, Environment.NewLine, __currentCmdInfo.Tag);
                    msg = string.Format(@""{0}{1}  Line:       {2}"", msg, Environment.NewLine, __currentCmdInfo.Line);
                    msg = string.Format(@""{0}{1}  Position:   {2}"", msg, Environment.NewLine, __currentCmdInfo.Position);
                    msg = string.Format(@""{0}{1}  Source:     {2}"", msg, Environment.NewLine, __currentCmdInfo.Source);
                    msg = string.Format(@""{0}{1}  Omit Scope: {2}"", msg, Environment.NewLine, __currentCmdInfo.OmitTagScope);
                }
                
                // Macros
                msg = string.Format(""{0}{1}{1}Macros:"", msg, Environment.NewLine);
                foreach (string key in __macros.Keys)
                {
                    string importInfo = """";
                    if (!string.IsNullOrEmpty(__macros[key]))
                    {
                        importInfo = string.Format(@"" imported from """"{0}"""""", __macros[key]);
                    }
                    msg = string.Format(@""{0}{1}  """"{2}""""{3}"", msg, Environment.NewLine, key, importInfo);
                }
                
                throw new Exception(msg, ex);
            }
        }
        
        private const string DEFAULT_VALUE = ""${defaultvalue}"";
        private static readonly Regex _re_needs_escape = new Regex(@""[&<>""""\']"");
        private static readonly Regex _re_amp = new Regex(@""&(?!([A-Za-z]+|#[0-9]+);)"");
        private delegate void MacroDelegate();
        
        private class ProgramNamespace
        {
            public Dictionary<string, MacroDelegate> macros = new Dictionary<string, MacroDelegate>();
        }
        
        private class CommandInfo
        {
            public string CommandName;
            public string Tag;
            public int Line;
            public int Position;
            public string Source;
            public bool OmitTagScope;
        }
        
        private class Attr
        {
            public string Name;
            public string Value;
            public string Eq;
            public string Quote;
            public string QuoteEntity;
        }
        
        private static string Escape(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;
            if (!_re_needs_escape.IsMatch(str))
                return str;
            if (str.IndexOf('&') >= 0)
            {
                if (str.IndexOf(';') >= 0)
                    str = _re_amp.Replace(str, ""&amp;"");
                else
                    str = str.Replace(""&"", ""&amp;"");
            }
            if (str.IndexOf('>') >= 0)
                str = str.Replace(""<"", ""&lt;"");
            if (str.IndexOf('>') >= 0)
                str = str.Replace("">"", ""&gt;"");
            return str;
        }
        
        private static string EscapeAttrValue(string str, string quote, string quoteEntity)
        {
            if (string.IsNullOrEmpty(str))
                return str;
            str = Escape(str);
            if (!string.IsNullOrEmpty(quote) && str.IndexOf(quote) >= 0)
                str = str.Replace(quote, quoteEntity);
            return str;
        }
        
        private static string FormatResult(object result, CultureInfo culture)
        {
            IFormattable formattable = result as IFormattable;
            string resultValue = """";
            if (formattable != null)
                resultValue = formattable.ToString("""", culture);
            else
                resultValue = result.ToString();
            return resultValue;
        }
        
        private static bool IsDefaultValue(object obj)
        {
            if ((obj is string) && ((string)obj) == DEFAULT_VALUE)
            {
                return true;
            }
            return false;
        }
        
        private static bool IsFalseResult(object obj)
        {
            if (obj == null)
            {
                // Value was Nothing
                return true;
            }
            if (obj is bool)
            {
                return ((bool)obj) == false;
            }
            if (obj is int)
            {
                return ((int)obj) == 0;
            }
            if (obj is float)
            {
                return ((float)obj) == 0;
            }
            if (obj is double)
            {
                return ((double)obj) == 0;
            }
            if (obj is string)
            {
                return string.IsNullOrEmpty(((string)obj));
            }
            if (obj is IEnumerable)
            {
                return ((IEnumerable)obj).GetEnumerator().MoveNext() == false;
            }
            // Everything else is true, so we return false!
            return false;
        }
        
        private class RepeatVariable
        {
            private IEnumerable sequence;
            private int position;
            private bool iterStatus;
            public RepeatVariable(IEnumerable sequence)
            {
                this.sequence = sequence;
                this.position = -1;
            }
            public void UpdateStatus(bool iterStatus)
            {
                this.position++;
                this.iterStatus = iterStatus;
            }
            public int length
            {
                get
                {
                    if (this.sequence is ICollection)
                        return ((ICollection)this.sequence).Count;
                    if (this.sequence is string)
                        return ((string)this.sequence).Length;
                    return 0;
                }
            }
            public int index { get { return this.position; } }
            public int number { get { return this.position + 1; } }
            public bool even
            {
                get
                {
                    if ((this.position % 2) != 0)
                        return false;
                    return true;
                }
            }
            public bool odd
            {
                get
                {
                    if ((this.position % 2) == 0)
                        return false;
                    return true;
                }
            }
            public bool start
            {
                get
                {
                    if (this.position == 0)
                        return true;
                    return false;
                }
            }
            public bool end { get { return this.iterStatus == false; } }
            public string letter
            {
                get
                {
                    string result = """";
                    int nextCol = this.position;
                    if (nextCol == 0)
                        return ""a"";
                    while (nextCol > 0)
                    {
                        int tmp = nextCol;
                        nextCol = tmp / 26;
                        int thisCol = tmp % 26;
                        result = ((char)(((int)'a') + thisCol)).ToString() + result;
                    }
                    return result;
                }
            }
            public string Letter { get { return this.letter.ToUpper(); } }
            public string roman
            {
                get
                {
                    Dictionary<string, int> romanNumeralList = new Dictionary<string, int>()
                    {
                        { ""m"", 1000 }
                        ,{ ""cm"", 900 }
                        ,{ ""d"", 500 }
                        ,{ ""cd"", 400 }
                        ,{ ""c"", 100 }
                        ,{ ""xc"", 90 }
                        ,{ ""l"", 50 }
                        ,{ ""xl"", 40 }
                        ,{ ""x"", 10 }
                        ,{ ""ix"", 9 }
                        ,{ ""v"", 5 }
                        ,{ ""iv"", 4 }
                        ,{ ""i"", 1 }
                    };
                    if (this.position > 3999)
                        return "" "";
                    int num = this.position + 1;
                    string result = """";
                    foreach (KeyValuePair<string, int> kv in romanNumeralList)
                    {
                        while (num >= kv.Value)
                        {
                            result += kv.Key;
                            num -= kv.Value;
                        }
                    }
                    return result;
                }
            }
            public string Roman { get { return this.roman.ToUpper(); } }
        }
    }
}
";
		const string MAIN_PROGRAM_NAMESPACE = "template";
		const string DEFAULT_VALUE_EXPRESSION = "default";

		protected class Scope
		{
			public string ID;
			public Stack<SubScope> SubScope = new Stack<SubScope>();
			public bool Interpolation { get; set; }
		}

		protected class SubScope
		{
			public string ID;
		}

		protected class RepeatScope : SubScope
		{
			public string VarName;
		}

		protected Dictionary<string, string> typeNamesCache;
		protected List<string> globalNames;
		protected StringBuilder globalsBody;
		protected string globalsBodyTabs;
		protected StringBuilder rendererBody;
		protected string rendererBodyTabs;
		protected Scope currentScope;
		protected Stack<Scope> scopeStack;

		public CodeGenerator()
		{
			typeNamesCache = new Dictionary<string, string>();
			globalNames = new List<string>();
			globalsBody = new StringBuilder();
			globalsBodyTabs = "                ";
			rendererBody = new StringBuilder();
			rendererBodyTabs = "                ";
			scopeStack = new Stack<Scope>();
		}

		public string GenerateCode(TemplateInfo ti)
		{
			scopeStack = new Stack<Scope>();

			//----------------
			// Process globals
			//----------------

			if (ti.GlobalsTypes != null)
			{
				foreach (string varName in ti.GlobalsTypes.Keys)
				{
					Type type = ti.GlobalsTypes[varName];
					if (type != null)
					{
						string typeName = Utils.GetFullTypeName(type, typeNamesCache);
						globalNames.Add(varName);
						WriteToGlobals(@"{0} {1} = ({0})globals[""{1}""];", typeName, varName);
					}
				}
			}

			//-----------------------
			// Process macro commands
			//-----------------------

			List<string> programNamespaces = new List<string>();

			// Process main program macro commands
			ProcessProgramMacros(ti.MainProgram, MAIN_PROGRAM_NAMESPACE, programNamespaces);

			// Process imported programs macro commands
			foreach (string destNs in ti.ImportedNamespaces.Keys)
			{
				foreach (string templatePath in ti.ImportedNamespaces[destNs])
				{
					Program importedProgram = ti.ImportedPrograms[templatePath];
					ProcessProgramMacros(importedProgram, destNs, programNamespaces);
				}
			}

			// Main template macros are also in the global namespace
			WriteToGlobals(@"macros = {0}.macros;", MAIN_PROGRAM_NAMESPACE);

			//--------------------------------------------------------
			// Process main program commands (ignoring macro commands)
			//--------------------------------------------------------

			WriteToBody(@"//=======================");
			WriteToBody(@"// Main template program:");
			WriteToBody(@"//=======================");
			WriteToBody(@"");
			WriteToBody(@"__CleanProgram();");
			WriteToBody(@"");
			HandleCommands(ti.MainProgram.ProgramCommands);

			//----------------------------
			// Resolve required namespaces
			//----------------------------

			// Default namespaces
			List<string> namespacesList = new List<string>()
			{
				"System",
				"System.IO",
				"System.Linq",
				"System.Text",
				"System.Text.RegularExpressions",
				"System.Collections",
				"System.Collections.Generic",
				"System.Security.Permissions",
				"System.Security",
				"System.Globalization"
			};

			// Find all namespaces with extension methods in assemblies where global types are defined
			List<string> assemblies = new List<string>();
			if (ti.GlobalsTypes != null)
			{
				foreach (string varName in ti.GlobalsTypes.Keys)
				{
					Type type = ti.GlobalsTypes[varName];
					if (type != null)
					{
						if (!assemblies.Contains(type.Assembly.Location) &&
							!assemblies.Contains(Path.GetFileName(type.Assembly.Location)))
						{
							// Check if assembly has defined "ExtensionAttribute"
							Utils.GetExtensionMethodNamespaces(type.Assembly, namespacesList);
							assemblies.Add(type.Assembly.Location);

							// Referenced assemblies
							foreach (AssemblyName assemblyName in type.Assembly.GetReferencedAssemblies())
							{
								Assembly assembly = AppDomain.CurrentDomain.Load(assemblyName);
								if (!assemblies.Contains(assembly.Location) &&
									!assemblies.Contains(Path.GetFileName(assembly.Location)))
								{
									Utils.GetExtensionMethodNamespaces(assembly, namespacesList);
									assemblies.Add(assembly.Location);
								}
							}
						}
					}
				}
			}

			// Find all namespaces with extension methods in referenced assemblies
			if (ti.ReferencedAssemblies != null)
			{
				foreach (Assembly refAsm in ti.ReferencedAssemblies)
				{
					if (!assemblies.Contains(refAsm.Location) &&
						!assemblies.Contains(Path.GetFileName(refAsm.Location)))
					{
						Utils.GetExtensionMethodNamespaces(refAsm, namespacesList);
						assemblies.Add(refAsm.Location);
					}
				}
			}

			// Create list of "usings"
			string usings = "";
			foreach (string ns in namespacesList)
			{
				usings = string.Format("{0}using {1};{2}", usings, ns, Environment.NewLine);
			}

			//-------------------------
			// Generate template source
			//-------------------------

			string templateSource = FileBodyTemplate.
				Replace("${defaultvalue}", Constants.DEFAULT_VALUE).
				Replace("${usings}", usings).
				Replace("${template_hash}", ti.TemplateKey).
				Replace("${globals}", globalsBody.ToString()).
				Replace("${body}", rendererBody.ToString());
			return templateSource;
		}

		private void ProcessProgramMacros(Program templateProgram, string programNamespace, List<string> programNamespaces)
		{
			if (templateProgram.Macros != null)
			{
				if (!programNamespaces.Contains(programNamespace))
				{
					// Check if other global var is defined with the name as this template
					if (globalNames.Contains(programNamespace))
					{
						throw new InvalidOperationException(string.Format(@"Failed to process macros in namespace ""{0}"".
Global variable with namespace name allready exists.", programNamespace));
					}
					globalNames.Add(programNamespace);
					programNamespaces.Add(programNamespace);
					WriteToGlobals(@"ProgramNamespace {0} = new ProgramNamespace();", programNamespace);
				}

				foreach (IProgram macro in templateProgram.Macros.Values)
				{
					// Create macro delegate
					WriteToBody(@"//====================");
					WriteToBody(@"// Macro: ""{0}.{1}""", programNamespace, macro.Name);
					WriteToBody(@"// Source: ""{0}""", macro.TemplatePath);
					WriteToBody(@"//====================");
					WriteToBody(@"MacroDelegate macro_{0}_{1} = delegate()", programNamespace, macro.Name);
					WriteToBody(@"{{");
					rendererBodyTabs += "    ";
					WriteToBody(@"__CleanProgram();");

					// Process METAL_DEFINE_PARAM commands
					HandleCommands(macro.ProgramCommands.Where(c => c.CommandType == CommandType.METAL_DEFINE_PARAM));

					// Process macro commands and ignore METAL_DEFINE_PARAM commands
					HandleCommands(macro.ProgramCommands.Where(c => c.CommandType != CommandType.METAL_DEFINE_PARAM));

					// Finalize macro delegate
					rendererBodyTabs = rendererBodyTabs.Remove(rendererBodyTabs.Length - 5, 4);
					WriteToBody(@"}};");
					WriteToBody(@"{0}.macros.Add(""{1}"", macro_{0}_{1});", programNamespace, macro.Name);
					WriteToBody(@"__macros.Add(@""{0}.{1}"", @""{2}"");", programNamespace, macro.Name, macro.TemplatePath);
					WriteToBody(@"");
				}
			}
		}

		private void WriteCmdInfo(ICommand command)
		{
			WriteToBody("");
			WriteToBody("__currentCmdInfo = new CommandInfo();");
			WriteToBody(@"__currentCmdInfo.CommandName = ""{0}"";", Enum.GetName(typeof(CommandType), command.CommandType));
			if (command.Tag != null)
			{
				WriteToBody(@"__currentCmdInfo.Tag = @""{0}"";", command.Tag.ToString().Replace(Environment.NewLine, "").Replace(@"""", @""""""));
				WriteToBody(@"__currentCmdInfo.Line = {0};", command.Tag.LineNumber);
				WriteToBody(@"__currentCmdInfo.Position = {0};", command.Tag.LinePosition);
				WriteToBody(@"__currentCmdInfo.Source = @""{0}"";", command.Tag.SourcePath);
			}
			WriteToBody("");
		}

		static readonly Regex _str_expr_regex = new Regex(@"(?<!\\)\$({(?<expression>.*)})", RegexOptions.Singleline);

		protected string FormatStringExpression(string expression)
		{
			List<string> formatNodes = new List<string>();
			List<string> formatArgs = new List<string>();
			string text = expression;
			while (!string.IsNullOrEmpty(text))
			{
				string matched = text;
				var m = _str_expr_regex.Match(matched);
				if (!m.Success)
				{
					formatNodes.Add(text.Replace(@"""", @"""""").Replace(@"{", @"{{").Replace(@"}", @"}}"));
					break;
				}
				string part = text.Substring(0, m.Index);
				text = text.Substring(m.Index);
				if (!string.IsNullOrEmpty(part))
				{
					formatNodes.Add(part.Replace(@"""", @"""""").Replace(@"{", @"{{").Replace(@"}", @"}}"));
				}
				while (true)
				{
					string str = m.Groups["expression"].Value;
					try
					{
						string s = FormatExpression(str);
						formatNodes.Add(string.Format("{{{0}}}", formatArgs.Count));
						formatArgs.Add(s);
						break;
					}
					catch (Exception ex)
					{
						if (m.Length == 0)
							throw;
						matched = matched.Substring(m.Index, m.Length - 1);
						m = _str_expr_regex.Match(matched);
						if (!m.Success)
							throw;
					}
				}
				text = text.Substring(m.Length);
			}
			string result = string.Format(@"string.Format(@""{0}""{1}{2})",
				string.Join("", formatNodes.ToArray()),
				formatArgs.Count > 0 ? ", " : "",
				string.Join(", ", formatArgs.ToArray()));
			return result;
		}

		protected string FormatCSharpExpression(string expression)
		{
			using (var parser = ParserFactory.CreateParser(SupportedLanguage.CSharp, new StringReader(expression + ";")))
			{
				var astExpr = parser.ParseExpression();
				if (parser.Errors.Count > 0)
				{
					throw new TemplateParseException(null, string.Format("{0}\n{1}", expression, parser.Errors.ErrorOutput));
				}
			}
			return expression;
		}

		protected string FormatExpression(string expression)
		{
			// Expression: "default"
			if (expression.Trim(' ') == DEFAULT_VALUE_EXPRESSION)
			{
				return "DEFAULT_VALUE";
			}

			// Expression: "string:"
			if (expression.TrimStart(' ').StartsWith("string:"))
			{
				return FormatStringExpression(expression.TrimStart(' ').Substring("string:".Length));
			}

			// Expression: "csharp:"
			if (expression.TrimStart(' ').StartsWith("csharp:"))
			{
				expression = expression.TrimStart(' ').Substring("csharp:".Length);
			}
			return FormatCSharpExpression(expression);
		}

		protected void WriteToGlobals(string format, params object[] args)
		{
			if (args != null)
				format = string.Format(format, args);
			globalsBody.AppendFormat(
				@"{0}{1}{2}",
				Environment.NewLine, globalsBodyTabs, format);
		}

		protected void WriteToBody(string format, params object[] args)
		{
			if (args != null)
				format = string.Format(format, args);
			rendererBody.AppendFormat(
				@"{0}{1}{2}",
				Environment.NewLine, rendererBodyTabs, format);
		}

		static string SafeVariableName(string str)
		{
			string name = "";
			for (int i = 0; i < str.Length; i++)
			{
				if (char.IsLetterOrDigit(str, i))
					name += str[i];
				else
					name += '_';
			}
			return name;
		}

		#region AbstractProgramInterpreter implementation

		protected override void Handle_META_INTERPOLATION(ICommand command)
		{
			METAInterpolation interpolationCmd = (METAInterpolation)command;
			currentScope.Interpolation = interpolationCmd.Enabled;
		}

		protected override void Handle_METAL_USE_MACRO(ICommand command)
		{
			// Evaluates the expression, if it resolves to a SubTemplate it then places
			// the slotMap into currentSlots and then jumps to the end tag

			METALUseMacro useMacroCmd = (METALUseMacro)command;

			string macroExpression = useMacroCmd.Expression;
			Dictionary<string, ProgramSlot> slots = useMacroCmd.Slots;
			List<METALDefineParam> parameters = useMacroCmd.Parameters;

			string scopeID = currentScope.ID;

			// Start SubScope
			string subScopeID = Guid.NewGuid().ToString().Replace("-", "");
			currentScope.SubScope.Push(new SubScope() { ID = subScopeID });

			WriteCmdInfo(command);

			string expression = FormatExpression(macroExpression);
			WriteToBody(@"object use_macro_delegate_{0} = {1};", subScopeID, expression);
			WriteToBody(@"if (use_macro_delegate_{0} != null && use_macro_delegate_{0} is MacroDelegate)", subScopeID);
			WriteToBody(@"{{");
			rendererBodyTabs += "    ";
			WriteToBody(@"__outputTag = 0;");
			WriteToBody(@"__tagContent = use_macro_delegate_{0};", subScopeID);
			WriteToBody(@"__tagContentType = 1;");
			WriteToBody(@"__slotMap = new Dictionary<string, MacroDelegate>();");

			// Set macro params
			foreach (METALDefineParam param in parameters)
			{
				WriteToBody(@"__paramMap[""{0}""] = {1};", param.Name, FormatExpression(param.Expression));
			}

			// Expand slots (SubTemplates)
			foreach (string slotName in slots.Keys)
			{
				ProgramSlot slot = slots[slotName];

				string slotID = Guid.NewGuid().ToString().Replace("-", "");

				// Create slot delegate
				WriteToBody(@"");
				WriteToBody(@"//====================");
				WriteToBody(@"// Slot: ""{0}""", slotName);
				WriteToBody(@"//====================");
				WriteToBody(@"MacroDelegate slot_{0}_delegate_{1} = delegate()", slotName, slotID);
				WriteToBody(@"{{");
				rendererBodyTabs += "    ";

				// Process slot commands
				HandleCommands(slot.ProgramCommands);

				// Finalize slot delegate
				rendererBodyTabs = rendererBodyTabs.Remove(rendererBodyTabs.Length - 5, 4);
				WriteToBody(@"}};");
				WriteToBody(@"__slotMap[""{0}""] = slot_{0}_delegate_{1};", slotName, slotID);
				WriteToBody(@"");
			}

			// Go to end tag
			WriteToBody(@"");
			WriteToBody(@"// NOTE: WE JUMP STRAIGHT TO THE END TAG, NO OTHER TAL/METAL COMMANDS ARE EVALUATED");
			WriteToBody(@"goto TAL_ENDTAG_ENDSCOPE_{0};", scopeID);
		}

		protected override void Handle_METAL_DEFINE_SLOT(ICommand command)
		{
			// If the slotName is filled then that is used, otherwise the original content is used.
			METALDefineSlot defineSlotCmd = (METALDefineSlot)command;

			string slotName = defineSlotCmd.SlotName;

			string scopeID = currentScope.ID;

			WriteCmdInfo(command);

			WriteToBody(@"if (__currentSlots.ContainsKey(""{0}""))", slotName);
			WriteToBody(@"{{");
			WriteToBody("     // This slot is filled, so replace us with that content");
			WriteToBody(@"    __outputTag = 0;");
			WriteToBody(@"    __tagContent = __currentSlots[""{0}""];", slotName);
			WriteToBody(@"    __tagContentType = 1;");
			WriteToBody(@"    ");
			WriteToBody(@"    // Output none of our content or the existing content");
			WriteToBody(@"    // NOTE: NO FURTHER TAL/METAL COMMANDS ARE EVALUATED");
			WriteToBody(@"    goto TAL_ENDTAG_ENDSCOPE_{0};", scopeID);
			WriteToBody(@"}}");
		}

		protected override void Handle_METAL_DEFINE_PARAM(ICommand command)
		{
			METALDefineParam defineParamCmd = (METALDefineParam)command;

			WriteCmdInfo(defineParamCmd);

			string expression = FormatExpression(defineParamCmd.Expression);

			// Create param variable
			WriteToBody(@"{0} {1} = {2};", defineParamCmd.Type, defineParamCmd.Name, expression);
			WriteToBody(@"if (__currentParams.ContainsKey(""{0}""))", defineParamCmd.Name);
			WriteToBody(@"{{");
			WriteToBody("     // This param is filled");
			WriteToBody(@"    {0} = ({1})__currentParams[""{0}""];", defineParamCmd.Name, defineParamCmd.Type);
			WriteToBody(@"}}");
		}

		protected override void Handle_TAL_DEFINE(ICommand command)
		{
			TALDefine defineCmd = (TALDefine)command;

			WriteCmdInfo(defineCmd);

			string expression = FormatExpression(defineCmd.Expression);
			if (defineCmd.Scope == TALDefine.VariableScope.Local)
			{
				// Create new local variable
				string body = string.Format(@"var {0} = {1};", defineCmd.Name, expression);
				WriteToBody(body);
			}
			else if (defineCmd.Scope == TALDefine.VariableScope.NonLocal)
			{
				// Set existing variable
				string body = string.Format(@"{0} = {1};", defineCmd.Name, expression);
				WriteToBody(body);
			}
			else
			{
				if (globalNames.Contains(defineCmd.Name))
				{
					// Set existing global variable
					string body = string.Format(@"{0} = {1};", defineCmd.Name, expression);
					WriteToBody(body);
				}
				else
				{
					// Create new global variable
					string body = string.Format(@"var {0} = {1};", defineCmd.Name, expression);
					globalNames.Add(defineCmd.Name);
					WriteToGlobals(body);
				}
			}
		}

		protected override void Handle_TAL_CONDITION(ICommand command)
		{
			// Conditionally continues with execution of all content contained by it.
			TALCondition conditionCmd = (TALCondition)command;

			string expression = conditionCmd.Expression;

			string scopeID = currentScope.ID;

			WriteCmdInfo(command);

			// Start SubScope
			expression = FormatExpression(expression);
			WriteToBody(@"if (IsFalseResult({0}))", expression);
			WriteToBody(@"{{");
			WriteToBody(@"    // Nothing to output - evaluated to false.");
			WriteToBody(@"    __outputTag = 0;");
			WriteToBody(@"    __tagContent = null;");
			WriteToBody(@"    goto TAL_ENDTAG_ENDSCOPE_{0};", scopeID);
			WriteToBody(@"}}");
		}

		protected override void Handle_TAL_REPEAT(ICommand command)
		{
			// Repeats anything in the cmndList
			TALRepeat repeatCmd = (TALRepeat)command;

			string varName = repeatCmd.Name;
			string expression = repeatCmd.Expression;

			// Start Repeat SubScope
			string repeatSubScopeID = Guid.NewGuid().ToString().Replace("-", "");
			currentScope.SubScope.Push(new RepeatScope() { ID = repeatSubScopeID, VarName = varName });

			WriteCmdInfo(command);

			expression = FormatExpression(expression);

			WriteToBody(@"// Backup the current attributes for this tag");
			WriteToBody(@"Dictionary<string, Attr> __currentAttributesCopy_{0} = new Dictionary<string, Attr>(__currentAttributes);", repeatSubScopeID);
			WriteToBody(@"");
			WriteToBody(@"var repeat_expression_{0} = {1};", repeatSubScopeID, expression);
			WriteToBody(@"var enumerable_{0}_{1} = repeat_expression_{1};", varName, repeatSubScopeID);
			WriteToBody(@"var enumerator_{0}_{1} = enumerable_{0}_{1}.GetEnumerator();", varName, repeatSubScopeID);
			WriteToBody(@"bool enumerator_status_{0}_{1} = enumerator_{0}_{1}.MoveNext();", varName, repeatSubScopeID);
			WriteToBody(@"bool enumerator_isdefault_{0}_{1} = false;", varName, repeatSubScopeID);
			WriteToBody(@"bool enumerator_isfirst_{0}_{1} = true;", varName, repeatSubScopeID);
			WriteToBody(@"if (IsDefaultValue(repeat_expression_{0}))", repeatSubScopeID);
			WriteToBody(@"{{");
			WriteToBody(@"    // Stop after first enumeration, so only default content is rendered");
			WriteToBody(@"    enumerator_status_{0}_{1} = false;", varName, repeatSubScopeID);
			WriteToBody(@"    enumerator_isdefault_{0}_{1} = true;", varName, repeatSubScopeID);
			WriteToBody(@"}}");
			WriteToBody(@"else");
			WriteToBody(@"{{");
			WriteToBody(@"    repeat[""{0}""] = new RepeatVariable(enumerable_{0}_{1});", varName, repeatSubScopeID);
			WriteToBody(@"}}");
			WriteToBody(@"do");
			WriteToBody(@"{{");
			WriteToBody(@"    __outputTag = 1;");
			WriteToBody(@"    __tagContent = null;");
			WriteToBody(@"    __tagContentType = 0;");
			WriteToBody(@"    __moveToEndTag = false;");
			WriteToBody(@"    ");
			WriteToBody(@"    // Skip repeat, if there is nothing to enumerate");
			WriteToBody(@"    if (enumerator_status_{0}_{1} == false &&", varName, repeatSubScopeID);
			WriteToBody(@"        enumerator_isfirst_{0}_{1} == true &&", varName, repeatSubScopeID);
			WriteToBody(@"        enumerator_isdefault_{0}_{1} == false)", varName, repeatSubScopeID);
			WriteToBody(@"    {{");
			WriteToBody(@"        goto END_REPEAT_{0};", repeatSubScopeID);
			WriteToBody(@"    }}");
			WriteToBody(@"    enumerator_isfirst_{0}_{1} = false;", varName, repeatSubScopeID);
			WriteToBody(@"    ");
			WriteToBody(@"    var {0} = enumerator_{0}_{1}.Current;", varName, repeatSubScopeID);
			WriteToBody(@"    if (!enumerator_isdefault_{0}_{1})", varName, repeatSubScopeID);
			WriteToBody(@"    {{");
			WriteToBody(@"        enumerator_status_{0}_{1} = enumerator_{0}_{1}.MoveNext();", varName, repeatSubScopeID);
			WriteToBody(@"        repeat[""{0}""].UpdateStatus(enumerator_status_{0}_{1});", varName, repeatSubScopeID);
			WriteToBody(@"    }}");

			rendererBodyTabs += "    ";
		}

		protected override void Handle_TAL_CONTENT(ICommand command)
		{
			TALContent contentCmd = (TALContent)command;

			string expression = contentCmd.Expression;
			bool structure = contentCmd.Structure;

			string scopeID = currentScope.ID;

			WriteCmdInfo(command);

			expression = FormatExpression(expression);
			WriteToBody(@"object content_expression_result_{0} = {1};", scopeID, expression);
			WriteToBody(@"");
			WriteToBody(@"if (content_expression_result_{0} == null)", scopeID);
			WriteToBody(@"{{");
			WriteToBody(@"    // Output none of our content or the existing content, but potentially the tags");
			WriteToBody(@"    __moveToEndTag = true;", scopeID);
			WriteToBody(@"}}");
			WriteToBody(@"else if (!IsDefaultValue(content_expression_result_{0}))", scopeID);
			WriteToBody(@"{{");
			WriteToBody(@"    // We have content, so let's suppress the natural content and output this!");
			WriteToBody(@"    __tagContent = {0};", expression);
			WriteToBody(@"    __tagContentType = {0};", structure ? 1 : 0);
			WriteToBody(@"    __moveToEndTag = true;");
			WriteToBody(@"}}");
		}

		protected override void Handle_TAL_REPLACE(ICommand command)
		{
			TALReplace replaceCmd = (TALReplace)command;

			string expression = replaceCmd.Expression;
			bool structure = replaceCmd.Structure;

			string scopeID = currentScope.ID;

			WriteCmdInfo(command);

			expression = FormatExpression(expression);
			WriteToBody(@"object content_expression_result_{0} = {1};", scopeID, expression);
			WriteToBody(@"");
			WriteToBody(@"if (content_expression_result_{0} == null)", scopeID);
			WriteToBody(@"{{");
			WriteToBody(@"    // Only output tags if this is a content not a replace");
			WriteToBody(@"    __outputTag = 0;");
			WriteToBody(@"    // Output none of our content or the existing content, but potentially the tags");
			WriteToBody(@"    __moveToEndTag = true;", scopeID);
			WriteToBody(@"}}");
			WriteToBody(@"else if (!IsDefaultValue(content_expression_result_{0}))", scopeID);
			WriteToBody(@"{{");
			WriteToBody(@"    // Replace content - do not output tags");
			WriteToBody(@"    __outputTag = 0;");
			WriteToBody(@"    __tagContent = {0};", expression);
			WriteToBody(@"    __tagContentType = {0};", structure ? 1 : 0);
			WriteToBody(@"    __moveToEndTag = true;");
			WriteToBody(@"}}");
		}

		protected override void Handle_CMD_OUTPUT(ICommand command)
		{
			CMDOutput outputCmd = (CMDOutput)command;

			string data = outputCmd.Data;

			WriteCmdInfo(command);

			if (currentScope != null && currentScope.Interpolation)
			{
				string expression = FormatStringExpression(data);
				WriteToBody(@"output.Write({0});", expression);
			}
			else
				WriteToBody(@"output.Write(@""{0}"");", data.Replace(@"""", @""""""));
		}

		protected override void Handle_TAL_OMITTAG(ICommand command)
		{
			// Conditionally turn off tag output
			TALOmitTag omitTagCmd = (TALOmitTag)command;

			string expression = omitTagCmd.Expression;

			WriteCmdInfo(command);

			expression = FormatExpression(expression);
			WriteToBody(@"if (!IsFalseResult({0}))", expression);
			WriteToBody(@"{{");
			WriteToBody(@"    // Turn tag output off");
			WriteToBody(@"    __outputTag = 0;");
			WriteToBody(@"}}");
		}

		protected override void Handle_CMD_START_SCOPE(ICommand command)
		{
			// Pushes the current state onto the stack, and sets up the new state
			CMDStartScope startScopeCmd = (CMDStartScope)command;

			WriteCmdInfo(command);

			scopeStack.Push(currentScope);
			currentScope = new Scope()
			{
				ID = Guid.NewGuid().ToString().Replace("-", ""),
				Interpolation = true,
				SubScope = new Stack<SubScope>()
			};
			WriteToBody("");
			WriteToBody("// Start scope: {0}", currentScope.ID);
			WriteToBody("{{");
			rendererBodyTabs += "    ";

			string scopeID = currentScope.ID;

			WriteToBody("");
			WriteToBody(@"Dictionary<string, Attr> __currentAttributes_{0} = new Dictionary<string, Attr>();", scopeID);
			WriteToBody(@"");
			WriteToBody(@"List<object> push_scope_{0} = new List<object>()", scopeID);
			WriteToBody(@"{{");
			WriteToBody(@"    __moveToEndTag,");
			WriteToBody(@"    __outputTag,");
			WriteToBody(@"    __currentAttributes,");
			WriteToBody(@"    __tagContent,");
			WriteToBody(@"    __tagContentType");
			WriteToBody(@"}};");
			WriteToBody(@"");
			WriteToBody(@"__scopeStack.Push(push_scope_{0});", scopeID);
			WriteToBody(@"");
			WriteToBody(@"__moveToEndTag = false;");
			WriteToBody(@"__outputTag = 1;");
			WriteToBody(@"__currentAttributes = __currentAttributes_{0};", scopeID);
			WriteToBody(@"__tagContent = null;");
			WriteToBody(@"__tagContentType = 1;");
		}

		protected override void Handle_TAL_ATTRIBUTES(ICommand command)
		{
			TALAttributes attributesCmd = (TALAttributes)command;

			List<TagAttribute> attributes = attributesCmd.Attributes;

			string scopeID = currentScope.ID;

			WriteCmdInfo(command);

			HashSet<string> attVarNames = new HashSet<string>();
			foreach (TagAttribute att in attributes)
			{
				WriteToBody("// Attribute: {0}", att.Name);
				string attVarName = SafeVariableName(att.Name);
				if (!attVarNames.Contains(attVarName))
				{
					attVarNames.Add(attVarName);
					WriteToBody(@"object attribute_{0}_{1} = null;", attVarName, scopeID);
				}
				if (string.IsNullOrEmpty(att.Value))
				{
					WriteToBody(@"if (!__currentAttributes.ContainsKey(""{0}""))", att.Name);
					WriteToBody(@"    __currentAttributes.Add(""{0}"", new Attr {{ Name = @""{0}"", Value = @"""", Eq = @""{1}"", Quote = @""{2}"", QuoteEntity = @""{3}"" }});",
						att.Name, att.Eq, att.Quote.Replace(@"""", @""""""), att.QuoteEntity);
				}
				else
				{
					string expression = "";
					if (att is TALTagAttribute)
						// This is TAL command attribute
						expression = FormatExpression(att.Value);
					else
					{
						// This is clean attribute (no TAL command)
						if (currentScope.Interpolation)
							// Interpolate attribute value
							expression = FormatStringExpression(att.Value);
						else
							// Get attribute value as raw string
							expression = string.Format(@"@""{0}""", att.Value.Replace(@"""", @""""""));
					}
					WriteToBody(@"try");
					WriteToBody(@"{{");
					WriteToBody(@"    attribute_{0}_{1} = {2};", attVarName, scopeID, expression);
					WriteToBody(@"}}");
					WriteToBody(@"catch (Exception ex)");
					WriteToBody(@"{{");
					WriteToBody(@"    attribute_{0}_{1} = null;", attVarName, scopeID);
					WriteToBody(@"}}");
					WriteToBody(@"if (attribute_{0}_{1} == null)", attVarName, scopeID);
					WriteToBody(@"    __currentAttributes.Remove(""{0}"");", att.Name);
					WriteToBody(@"else if (!IsDefaultValue(attribute_{0}_{1}))", attVarName, scopeID);
					WriteToBody(@"{{");
					WriteToBody(@"    if (!__currentAttributes.ContainsKey(""{0}""))", att.Name);
					WriteToBody(@"        __currentAttributes.Add(""{0}"", new Attr {{ Name = @""{0}"", Value = @"""", Eq = @""{1}"", Quote = @""{2}"", QuoteEntity = @""{3}"" }});",
						att.Name, att.Eq, att.Quote.Replace(@"""", @""""""), att.QuoteEntity);
					WriteToBody(@"    __currentAttributes[""{0}""].Value = FormatResult(attribute_{1}_{2}, culture);", att.Name, attVarName, scopeID);
					WriteToBody(@"}}");
				}
			}
		}

		protected override void Handle_CMD_START_TAG(ICommand command)
		{
			CMDStartTag startTagCmd = (CMDStartTag)command;

			string tagName = startTagCmd.Tag.Name;
			string tagSuffix = startTagCmd.Tag.Suffix;

			string scopeID = currentScope.ID;

			WriteCmdInfo(command);

			WriteToBody(@"if (__outputTag == 1)");
			WriteToBody(@"{{");
			WriteToBody(@"    output.Write(@""<"");");
			WriteToBody(@"    output.Write(@""{0}"");", tagName);
			WriteToBody(@"    foreach (var att in __currentAttributes.Values)");
			WriteToBody(@"    {{");
			WriteToBody(@"        output.Write(@"" {{0}}{{1}}{{2}}{{3}}{{2}}"", att.Name, att.Eq, att.Quote, EscapeAttrValue(att.Value, att.Quote, att.QuoteEntity));");
			WriteToBody(@"    }}");
			WriteToBody(@"    output.Write(@""{0}"");", tagSuffix);
			WriteToBody(@"}}");
			WriteToBody(@"");
			WriteToBody(@"if (__moveToEndTag == true)");
			WriteToBody(@"{{");
			WriteToBody(@"    goto TAL_ENDTAG_ENDSCOPE_{0};", scopeID);
			WriteToBody(@"}}");
		}

		protected override void Handle_CMD_ENDTAG_ENDSCOPE(ICommand command)
		{
			CMDEntTagEndScope entTagEndScopeCmd = (CMDEntTagEndScope)command;

			string tagName = entTagEndScopeCmd.Tag.Name;
			bool singletonTag = entTagEndScopeCmd.Tag.Singleton;

			WriteCmdInfo(command);

			string scopeID = currentScope.ID;

			WriteToBody("TAL_ENDTAG_ENDSCOPE_{0}:", scopeID);
			WriteToBody("");
			WriteToBody("// End tag: <{0}>", tagName);

			WriteToBody(@"if (__tagContent != null)");
			WriteToBody(@"{{");
			WriteToBody(@"    if (__tagContentType == 1)");
			WriteToBody(@"    {{");
			WriteToBody(@"        if (__tagContent is MacroDelegate)");
			WriteToBody(@"        {{");
			WriteToBody(@"            // Save our state!");
			WriteToBody(@"            __PushProgram();");
			WriteToBody(@"            // Execute macro or slot delegate");
			WriteToBody(@"            ((MacroDelegate)__tagContent)();");
			WriteToBody(@"            // Restore state");
			WriteToBody(@"            __PopProgram();");
			WriteToBody(@"            // End of the macro expansion (if any) so clear the slots and params");
			WriteToBody(@"            __slotMap = new Dictionary<string, MacroDelegate>();");
			WriteToBody(@"            __paramMap = new Dictionary<string, object>();");
			WriteToBody(@"        }}");
			WriteToBody(@"        else");
			WriteToBody(@"        {{");
			WriteToBody(@"            output.Write((string)__tagContent);");
			WriteToBody(@"        }}");
			WriteToBody(@"    }}");
			WriteToBody(@"    else");
			WriteToBody(@"    {{");
			WriteToBody(@"        output.Write(Escape(FormatResult(__tagContent, culture)));");
			WriteToBody(@"    }}");
			WriteToBody(@"}}");

			WriteToBody(@"if (__outputTag == 1)");
			WriteToBody(@"{{");
			WriteToBody(@"    // Do NOT output end tag if a singleton with no content");
			WriteToBody(@"    if (({0} == 1 && __tagContent == null) == false)", singletonTag ? 1 : 0);
			WriteToBody(@"    {{");
			WriteToBody(@"        output.Write(@""</{0}>"");", tagName);
			WriteToBody(@"    }}");
			WriteToBody(@"}}");

			while (currentScope.SubScope.Count > 0)
			{
				SubScope subScope = currentScope.SubScope.Pop();
				if (subScope is RepeatScope)
				{
					WriteToBody("");
					WriteToBody("// End sub-scope repeat: {0}", subScope.ID);
					WriteToBody(@"");
					WriteToBody(@"// Restore the current attributes");
					WriteToBody(@"__currentAttributes = new Dictionary<string, Attr>(__currentAttributesCopy_{1});", scopeID, subScope.ID);
					WriteToBody(@"");
					rendererBodyTabs = rendererBodyTabs.Remove(rendererBodyTabs.Length - 5, 4);
					WriteToBody("}}");
					WriteToBody(@"while (enumerator_status_{0}_{1} == true);", ((RepeatScope)subScope).VarName, subScope.ID);
					WriteToBody(@"END_REPEAT_{0}:", subScope.ID);
					WriteToBody(@"repeat[""{0}""] = null;", ((RepeatScope)subScope).VarName);
				}
				else
				{
					WriteToBody("");
					WriteToBody("// End sub-scope: {0}", subScope.ID);
					rendererBodyTabs = rendererBodyTabs.Remove(rendererBodyTabs.Length - 5, 4);
					WriteToBody("}}");
				}
			}

			WriteToBody("");

			WriteToBody(@"List<object> pop_scope_{0} = __scopeStack.Pop();", scopeID);
			WriteToBody(@"__moveToEndTag = (bool)pop_scope_{0}[0];", scopeID);
			WriteToBody(@"__outputTag = (int)pop_scope_{0}[1];", scopeID);
			WriteToBody(@"__currentAttributes = (Dictionary<string, Attr>)pop_scope_{0}[2];", scopeID);
			WriteToBody(@"__tagContent = (object)pop_scope_{0}[3];", scopeID);
			WriteToBody(@"__tagContentType = (int)pop_scope_{0}[4];", scopeID);

			WriteToBody("");
			WriteToBody("// End scope: {0}", scopeID);
			rendererBodyTabs = rendererBodyTabs.Remove(rendererBodyTabs.Length - 5, 4);
			WriteToBody("}}");

			currentScope = scopeStack.Pop();
		}

		protected override void Handle_METAL_FILL_SLOT(ICommand command)
		{
			throw new NotImplementedException();
		}

		protected override void Handle_METAL_DEFINE_MACRO(ICommand command)
		{
			throw new NotImplementedException();
		}

		protected override void Handle_METAL_FILL_PARAM(ICommand command)
		{
			throw new NotImplementedException();
		}

		protected override void Handle_METAL_IMPORT(ICommand command)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
