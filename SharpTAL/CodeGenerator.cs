//
// CodeGenerator.cs
//
// Author:
//   Roman Lacko (backup.rlacko@gmail.com)
//
// Copyright (c) 2010 - 2014 Roman Lacko
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
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using ICSharpCode.NRefactory.CSharp;

using SharpTAL.TemplateParser;
using SharpTAL.TemplateProgram;
using SharpTAL.TemplateProgram.Commands;

namespace SharpTAL
{
	public class CodeGenerator : AbstractProgramInterpreter, ICodeGenerator
	{
		protected static readonly string FileBodyTemplate =
			@"${usings}

namespace Templates
{
    [SecurityPermission(SecurityAction.PermitOnly, Execution = true)]
    public class Template_${template_hash}
    {
        public const string GENERATOR_VERSION = ""${generator_version}"";
        
        public static void Render(StreamWriter output, SharpTAL.IRenderContext context)
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
            var repeat = (SharpTAL.IRepeatDictionary)context[""repeat""];
            // TODO: get macros from context
            Dictionary<string, MacroDelegate> macros = new Dictionary<string, MacroDelegate>();
            
            FormatResult = (Func<object, string>)context[""__FormatResult""];
            IsFalseResult = (Func<object, bool>)context[""__IsFalseResult""];
            
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
        
        private static Func<object, string> FormatResult { get; set; }
        private static Func<object, bool> IsFalseResult { get; set; }
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
            public CommandInfo()
            {
            }
            public CommandInfo(string commandName)
            {
                CommandName = commandName;
            }
            public CommandInfo(string commandName, string tag, int line, int position, string source)
            {
                CommandName = commandName;
                Tag = tag;
                Line = line;
                Position = position;
                Source = source;
            }
            public string CommandName;
            public string Tag;
            public int Line;
            public int Position;
            public string Source;
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
        
        private static bool IsDefaultValue(object obj)
        {
            if ((obj is string) && ((string)obj) == DEFAULT_VALUE)
            {
                return true;
            }
            return false;
        }
    }
}
";
		private const string MainProgramNamespace = "template";
		private const string DefaultValueExpression = "default";

		private class Scope
		{
			public string Id;
			public Stack<SubScope> SubScopeStack = new Stack<SubScope>();
			public bool Interpolation { get; set; }
		}

		private class SubScope
		{
			public string Id;
		}

		private class RepeatScope : SubScope
		{
			public string VarName;
		}

		private readonly Dictionary<string, string> _typeNamesCache;
		private readonly List<string> _globalNames;
		private readonly StringBuilder _globalsBody;
		private readonly string _globalsBodyTabs;
		private readonly StringBuilder _rendererBody;
		private string _rendererBodyTabs;
		private Scope _currentScope;
		private Stack<Scope> _scopeStack;

		public CodeGenerator()
		{
			_typeNamesCache = new Dictionary<string, string>();
			_globalNames = new List<string>();
			_globalsBody = new StringBuilder();
			_globalsBodyTabs = "                ";
			_rendererBody = new StringBuilder();
			_rendererBodyTabs = "                ";
			_scopeStack = new Stack<Scope>();
		}

		public string GenerateCode(TemplateInfo ti)
		{
			_scopeStack = new Stack<Scope>();

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
						string typeName = Utils.GetFullTypeName(type, _typeNamesCache);
						_globalNames.Add(varName);
						WriteToGlobals(@"{0} {1} = ({0})context[""{1}""];", typeName, varName);
					}
				}
			}

			//-----------------------
			// Process macro commands
			//-----------------------

			var programNamespaces = new List<string>();

			// Process main program macro commands
			ProcessProgramMacros(ti.MainProgram, MainProgramNamespace, programNamespaces);

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
			WriteToGlobals(@"macros = {0}.macros;", MainProgramNamespace);

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
			var namespacesList = new List<string>
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
				"System.Globalization",
				"SharpTAL",
			};

			// Find all namespaces with extension methods in assemblies where global types are defined
			var assemblies = new List<string>();
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
				Replace("${generator_version}", GetType().Assembly.GetName().Version.ToString()).
				Replace("${defaultvalue}", Constants.DefaultValue).
				Replace("${usings}", usings).
				Replace("${template_hash}", ti.TemplateKey).
				Replace("${globals}", _globalsBody.ToString()).
				Replace("${body}", _rendererBody.ToString());
			return templateSource;
		}

		private void ProcessProgramMacros(Program templateProgram, string programNamespace, List<string> programNamespaces)
		{
			if (templateProgram.Macros != null)
			{
				if (!programNamespaces.Contains(programNamespace))
				{
					// Check if other global var is defined with the name as this template
					if (_globalNames.Contains(programNamespace))
					{
						throw new InvalidOperationException(string.Format(@"Failed to process macros in namespace ""{0}"".
Global variable with namespace name allready exists.", programNamespace));
					}
					_globalNames.Add(programNamespace);
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
					_rendererBodyTabs += "    ";
					WriteToBody(@"__CleanProgram();");

					// Process METAL_DEFINE_PARAM commands
					HandleCommands(macro.ProgramCommands.Where(c => c.CommandType == CommandType.MetalDefineParam));

					// Process macro commands and ignore METAL_DEFINE_PARAM commands
					HandleCommands(macro.ProgramCommands.Where(c => c.CommandType != CommandType.MetalDefineParam));

					// Finalize macro delegate
					_rendererBodyTabs = _rendererBodyTabs.Remove(_rendererBodyTabs.Length - 5, 4);
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
			if (command.Tag != null)
			{
				WriteToBody(@"__currentCmdInfo = new CommandInfo(""{0}"", @""{1}"", {2}, {3}, @""{4}"");",
					Enum.GetName(typeof(CommandType), command.CommandType),
					command.Tag.ToString().Replace(Environment.NewLine, "").Replace(@"""", @""""""),
					command.Tag.LineNumber,
					command.Tag.LinePosition,
					command.Tag.SourcePath);
			}
			else
			{
				WriteToBody(@"__currentCmdInfo = new CommandInfo(""{0}"");",
					Enum.GetName(typeof(CommandType), command.CommandType));
			}
			WriteToBody("");
		}

		protected string FormatCodeBlock(CmdCodeBlock codeBlock)
		{
			return FormatCSharpStatements(codeBlock.Code);
		}

		protected string FormatCSharpStatements(string code)
		{
			var parser = new CSharpParser();
			parser.ParseStatements(code);
			if (parser.HasErrors)
			{
				var errors = string.Join(Environment.NewLine, parser.Errors.Select(err => err.Message));
				throw new TemplateParseException(null, string.Format("{0}{1}{2}", code, Environment.NewLine, errors));
			}
			return code;
		}

		static readonly Regex StrExprRegex = new Regex(@"(?<!\\)\$({(?<expression>.*)})", RegexOptions.Singleline);

		protected string FormatStringExpression(string expression)
		{
			var formatNodes = new List<string>();
			var formatArgs = new List<string>();
			string text = expression;
			while (!string.IsNullOrEmpty(text))
			{
				string matched = text;
				var m = StrExprRegex.Match(matched);
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
					catch (Exception)
					{
						if (m.Length == 0)
							throw;
						matched = matched.Substring(m.Index, m.Length - 1);
						m = StrExprRegex.Match(matched);
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
			var parser = new CSharpParser();
			parser.ParseExpression(expression + ";");
			if (parser.HasErrors)
			{
				var errors = string.Join(Environment.NewLine, parser.Errors.Select(err => err.Message));
				throw new TemplateParseException(null, string.Format("{0}{1}{2}", expression, Environment.NewLine, errors));
			}
			return expression;
		}

		protected string FormatExpression(string expression)
		{
			// Expression: "default"
			if (expression.Trim(' ') == DefaultValueExpression)
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
			_globalsBody.AppendFormat(
				@"{0}{1}{2}",
				Environment.NewLine, _globalsBodyTabs, format);
		}

		protected void WriteToBody(string format, params object[] args)
		{
			if (args != null)
				format = string.Format(format, args);
			_rendererBody.AppendFormat(
				@"{0}{1}{2}",
				Environment.NewLine, _rendererBodyTabs, format);
		}

		protected void WriteToBodyNoFormat(string text)
		{
			_rendererBody.AppendFormat(
				@"{0}{1}{2}",
				Environment.NewLine, _rendererBodyTabs, text);
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
			var interpolationCmd = (MetaInterpolation)command;
			_currentScope.Interpolation = interpolationCmd.Enabled;
		}

		protected override void Handle_METAL_USE_MACRO(ICommand command)
		{
			// Evaluates the expression, if it resolves to a SubTemplate it then places
			// the slotMap into currentSlots and then jumps to the end tag

			var useMacroCmd = (MetalUseMacro)command;

			string macroExpression = useMacroCmd.Expression;
			Dictionary<string, ProgramSlot> slots = useMacroCmd.Slots;
			List<MetalDefineParam> parameters = useMacroCmd.Parameters;

			string scopeId = _currentScope.Id;

			// Start SubScope
			string subScopeId = Guid.NewGuid().ToString().Replace("-", "");
			_currentScope.SubScopeStack.Push(new SubScope { Id = subScopeId });

			WriteCmdInfo(command);

			string expression = FormatExpression(macroExpression);
			WriteToBody(@"object use_macro_delegate_{0} = {1};", subScopeId, expression);
			WriteToBody(@"if (use_macro_delegate_{0} != null && use_macro_delegate_{0} is MacroDelegate)", subScopeId);
			WriteToBody(@"{{");
			_rendererBodyTabs += "    ";
			WriteToBody(@"__outputTag = 0;");
			WriteToBody(@"__tagContent = use_macro_delegate_{0};", subScopeId);
			WriteToBody(@"__tagContentType = 1;");
			WriteToBody(@"__slotMap = new Dictionary<string, MacroDelegate>();");

			// Set macro params
			foreach (MetalDefineParam param in parameters)
			{
				WriteToBody(@"__paramMap[""{0}""] = {1};", param.Name, FormatExpression(param.Expression));
			}

			// Expand slots (SubTemplates)
			foreach (string slotName in slots.Keys)
			{
				ProgramSlot slot = slots[slotName];

				string slotId = Guid.NewGuid().ToString().Replace("-", "");

				// Create slot delegate
				WriteToBody(@"");
				WriteToBody(@"//====================");
				WriteToBody(@"// Slot: ""{0}""", slotName);
				WriteToBody(@"//====================");
				WriteToBody(@"MacroDelegate slot_{0}_delegate_{1} = delegate()", slotName, slotId);
				WriteToBody(@"{{");
				_rendererBodyTabs += "    ";

				// Process slot commands
				HandleCommands(slot.ProgramCommands);

				// Finalize slot delegate
				_rendererBodyTabs = _rendererBodyTabs.Remove(_rendererBodyTabs.Length - 5, 4);
				WriteToBody(@"}};");
				WriteToBody(@"__slotMap[""{0}""] = slot_{0}_delegate_{1};", slotName, slotId);
				WriteToBody(@"");
			}

			// Go to end tag
			WriteToBody(@"");
			WriteToBody(@"// NOTE: WE JUMP STRAIGHT TO THE END TAG, NO OTHER TAL/METAL COMMANDS ARE EVALUATED");
			WriteToBody(@"goto TAL_ENDTAG_ENDSCOPE_{0};", scopeId);
		}

		protected override void Handle_METAL_DEFINE_SLOT(ICommand command)
		{
			// If the slotName is filled then that is used, otherwise the original content is used.
			var defineSlotCmd = (MetalDefineSlot)command;

			string slotName = defineSlotCmd.SlotName;

			string scopeId = _currentScope.Id;

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
			WriteToBody(@"    goto TAL_ENDTAG_ENDSCOPE_{0};", scopeId);
			WriteToBody(@"}}");
		}

		protected override void Handle_METAL_DEFINE_PARAM(ICommand command)
		{
			var defineParamCmd = (MetalDefineParam)command;

			WriteCmdInfo(defineParamCmd);

			// Create param variable
			if (string.IsNullOrWhiteSpace(defineParamCmd.Expression))
			{
				WriteToBody(@"{0} {1} = default({0});", defineParamCmd.Type, defineParamCmd.Name);
			}
			else
			{
				string expression = FormatExpression(defineParamCmd.Expression);
				WriteToBody(@"{0} {1} = {2};", defineParamCmd.Type, defineParamCmd.Name, expression);
			}
			WriteToBody(@"if (__currentParams.ContainsKey(""{0}""))", defineParamCmd.Name);
			WriteToBody(@"{{");
			WriteToBody("     // This param is filled");
			WriteToBody(@"    {0} = ({1})__currentParams[""{0}""];", defineParamCmd.Name, defineParamCmd.Type);
			WriteToBody(@"}}");
		}

		protected override void Handle_TAL_DEFINE(ICommand command)
		{
			var defineCmd = (TalDefine)command;

			WriteCmdInfo(defineCmd);

			string expression = FormatExpression(defineCmd.Expression);
			if (defineCmd.Scope == TalDefine.VariableScope.Local)
			{
				// Create new local variable
				string body = string.Format(@"var {0} = {1};", defineCmd.Name, expression);
				WriteToBody(body);
			}
			else if (defineCmd.Scope == TalDefine.VariableScope.NonLocal)
			{
				// Set existing variable
				string body = string.Format(@"{0} = {1};", defineCmd.Name, expression);
				WriteToBody(body);
			}
			else
			{
				if (_globalNames.Contains(defineCmd.Name))
				{
					// Set existing global variable
					string body = string.Format(@"{0} = {1};", defineCmd.Name, expression);
					WriteToBody(body);
				}
				else
				{
					// Create new global variable
					string body = string.Format(@"var {0} = {1};", defineCmd.Name, expression);
					_globalNames.Add(defineCmd.Name);
					WriteToGlobals(body);
				}
			}
		}

		protected override void Handle_TAL_CONDITION(ICommand command)
		{
			// Conditionally continues with execution of all content contained by it.
			var conditionCmd = (TalCondition)command;

			string expression = conditionCmd.Expression;

			string scopeId = _currentScope.Id;

			WriteCmdInfo(command);

			// Start SubScope
			expression = FormatExpression(expression);
			WriteToBody(@"if (IsFalseResult({0}))", expression);
			WriteToBody(@"{{");
			WriteToBody(@"    // Nothing to output - evaluated to false.");
			WriteToBody(@"    __outputTag = 0;");
			WriteToBody(@"    __tagContent = null;");
			WriteToBody(@"    goto TAL_ENDTAG_ENDSCOPE_{0};", scopeId);
			WriteToBody(@"}}");
		}

		protected override void Handle_TAL_REPEAT(ICommand command)
		{
			// Repeats anything in the cmndList
			var repeatCmd = (TalRepeat)command;

			string varName = repeatCmd.Name;
			string expression = repeatCmd.Expression;

			// Start Repeat SubScope
			string repeatSubScopeId = Guid.NewGuid().ToString().Replace("-", "");
			_currentScope.SubScopeStack.Push(new RepeatScope { Id = repeatSubScopeId, VarName = varName });

			WriteCmdInfo(command);

			expression = FormatExpression(expression);

			WriteToBody(@"// Backup the current attributes for this tag");
			WriteToBody(@"Dictionary<string, Attr> __currentAttributesCopy_{0} = new Dictionary<string, Attr>(__currentAttributes);", repeatSubScopeId);
			WriteToBody(@"");
			WriteToBody(@"var enumerable_{0}_{1} = {2};", varName, repeatSubScopeId, expression);
			WriteToBody(@"var enumerator_{0}_{1} = enumerable_{0}_{1}.GetEnumerator();", varName, repeatSubScopeId);
			WriteToBody(@"bool isfirst_{0}_{1} = true;", varName, repeatSubScopeId);
			WriteToBody(@"bool islast_{0}_{1} = !enumerator_{0}_{1}.MoveNext();", varName, repeatSubScopeId);
			WriteToBody(@"bool isdefault_{0}_{1} = false;", varName, repeatSubScopeId);
			WriteToBody(@"if (IsDefaultValue(enumerable_{0}_{1}))", varName, repeatSubScopeId);
			WriteToBody(@"{{");
			WriteToBody(@"    // Stop after first enumeration, so only default content is rendered");
			WriteToBody(@"    islast_{0}_{1} = true;", varName, repeatSubScopeId);
			WriteToBody(@"    isdefault_{0}_{1} = true;", varName, repeatSubScopeId);
			WriteToBody(@"}}");
			WriteToBody(@"else");
			WriteToBody(@"    repeat[""{0}""] = new SharpTAL.RepeatItem(enumerable_{0}_{1});", varName, repeatSubScopeId);
			WriteToBody(@"do");
			WriteToBody(@"{{");
			WriteToBody(@"    __outputTag = 1;");
			WriteToBody(@"    __tagContent = null;");
			WriteToBody(@"    __tagContentType = 0;");
			WriteToBody(@"    __moveToEndTag = false;");
			WriteToBody(@"    ");
			WriteToBody(@"    // Skip repeat, if there is nothing to enumerate");
			WriteToBody(@"    if (isfirst_{0}_{1} &&", varName, repeatSubScopeId);
			WriteToBody(@"        islast_{0}_{1} &&", varName, repeatSubScopeId);
			WriteToBody(@"        isdefault_{0}_{1} == false)", varName, repeatSubScopeId);
			WriteToBody(@"    {{");
			WriteToBody(@"        goto END_REPEAT_{0};", repeatSubScopeId);
			WriteToBody(@"    }}");
			WriteToBody(@"    isfirst_{0}_{1} = false;", varName, repeatSubScopeId);
			WriteToBody(@"    ");
			WriteToBody(@"    var {0} = enumerator_{0}_{1}.Current;", varName, repeatSubScopeId);
			WriteToBody(@"    if (!isdefault_{0}_{1})", varName, repeatSubScopeId);
			WriteToBody(@"    {{");
			WriteToBody(@"        islast_{0}_{1} = !enumerator_{0}_{1}.MoveNext();", varName, repeatSubScopeId);
			WriteToBody(@"        repeat[""{0}""].next(islast_{0}_{1});", varName, repeatSubScopeId);
			WriteToBody(@"    }}");

			_rendererBodyTabs += "    ";
		}

		protected override void Handle_TAL_CONTENT(ICommand command)
		{
			var contentCmd = (TalContent)command;

			string expression = contentCmd.Expression;
			bool structure = contentCmd.Structure;

			string scopeId = _currentScope.Id;

			WriteCmdInfo(command);

			expression = FormatExpression(expression);
			WriteToBody(@"object content_expression_result_{0} = {1};", scopeId, expression);
			WriteToBody(@"");
			WriteToBody(@"if (content_expression_result_{0} == null)", scopeId);
			WriteToBody(@"{{");
			WriteToBody(@"    // Output none of our content or the existing content, but potentially the tags");
			WriteToBody(@"    __moveToEndTag = true;", scopeId);
			WriteToBody(@"}}");
			WriteToBody(@"else if (!IsDefaultValue(content_expression_result_{0}))", scopeId);
			WriteToBody(@"{{");
			WriteToBody(@"    // We have content, so let's suppress the natural content and output this!");
			WriteToBody(@"    __tagContent = {0};", expression);
			WriteToBody(@"    __tagContentType = {0};", structure ? 1 : 0);
			WriteToBody(@"    __moveToEndTag = true;");
			WriteToBody(@"}}");
		}

		protected override void Handle_TAL_REPLACE(ICommand command)
		{
			var replaceCmd = (TalReplace)command;

			string expression = replaceCmd.Expression;
			bool structure = replaceCmd.Structure;

			string scopeId = _currentScope.Id;

			WriteCmdInfo(command);

			expression = FormatExpression(expression);
			WriteToBody(@"object content_expression_result_{0} = {1};", scopeId, expression);
			WriteToBody(@"");
			WriteToBody(@"if (content_expression_result_{0} == null)", scopeId);
			WriteToBody(@"{{");
			WriteToBody(@"    // Only output tags if this is a content not a replace");
			WriteToBody(@"    __outputTag = 0;");
			WriteToBody(@"    // Output none of our content or the existing content, but potentially the tags");
			WriteToBody(@"    __moveToEndTag = true;", scopeId);
			WriteToBody(@"}}");
			WriteToBody(@"else if (!IsDefaultValue(content_expression_result_{0}))", scopeId);
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
			var outputCmd = (CmdOutput)command;

			string data = outputCmd.Data;

			WriteCmdInfo(command);

			if (_currentScope == null || (_currentScope != null && _currentScope.Interpolation))
			{
				string expression = FormatStringExpression(data);
				WriteToBody(@"output.Write({0});", expression);
			}
			else
				WriteToBody(@"output.Write(@""{0}"");", data.Replace(@"""", @""""""));
		}

		protected override void Handle_CMD_CODE_BLOCK(ICommand command)
		{
			var codeCmd = (CmdCodeBlock)command;

			WriteCmdInfo(command);

			string code = FormatCodeBlock(codeCmd);
			WriteToBodyNoFormat(code);
		}

		protected override void Handle_TAL_OMITTAG(ICommand command)
		{
			// Conditionally turn off tag output
			var omitTagCmd = (TalOmitTag)command;

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
			WriteCmdInfo(command);

			_scopeStack.Push(_currentScope);
			_currentScope = new Scope
			{
				Id = Guid.NewGuid().ToString().Replace("-", ""),
				Interpolation = true,
				SubScopeStack = new Stack<SubScope>()
			};
			WriteToBody("");
			WriteToBody("// Start scope: {0}", _currentScope.Id);
			WriteToBody("{{");
			_rendererBodyTabs += "    ";

			string scopeId = _currentScope.Id;

			WriteToBody("");
			WriteToBody(@"Dictionary<string, Attr> __currentAttributes_{0} = new Dictionary<string, Attr>();", scopeId);
			WriteToBody(@"");
			WriteToBody(@"List<object> push_scope_{0} = new List<object>()", scopeId);
			WriteToBody(@"{{");
			WriteToBody(@"    __moveToEndTag,");
			WriteToBody(@"    __outputTag,");
			WriteToBody(@"    __currentAttributes,");
			WriteToBody(@"    __tagContent,");
			WriteToBody(@"    __tagContentType");
			WriteToBody(@"}};");
			WriteToBody(@"");
			WriteToBody(@"__scopeStack.Push(push_scope_{0});", scopeId);
			WriteToBody(@"");
			WriteToBody(@"__moveToEndTag = false;");
			WriteToBody(@"__outputTag = 1;");
			WriteToBody(@"__currentAttributes = __currentAttributes_{0};", scopeId);
			WriteToBody(@"__tagContent = null;");
			WriteToBody(@"__tagContentType = 1;");
		}

		protected override void Handle_TAL_ATTRIBUTES(ICommand command)
		{
			var attributesCmd = (TalAttributes)command;

			List<TagAttribute> attributes = attributesCmd.Attributes;

			string scopeId = _currentScope.Id;

			WriteCmdInfo(command);

			var attVarNames = new HashSet<string>();
			foreach (TagAttribute att in attributes)
			{
				WriteToBody("// Attribute: {0}", att.Name);
				string attVarName = SafeVariableName(att.Name);
				if (!attVarNames.Contains(attVarName))
				{
					attVarNames.Add(attVarName);
					WriteToBody(@"object attribute_{0}_{1} = null;", attVarName, scopeId);
				}
				if (string.IsNullOrEmpty(att.Value))
				{
					WriteToBody(@"if (!__currentAttributes.ContainsKey(""{0}""))", att.Name);
					WriteToBody(@"    __currentAttributes.Add(""{0}"", new Attr {{ Name = @""{0}"", Value = @"""", Eq = @""{1}"", Quote = @""{2}"", QuoteEntity = @""{3}"" }});",
						att.Name, att.Eq, att.Quote.Replace(@"""", @""""""), att.QuoteEntity);
				}
				else
				{
					string expression;
					if (att is TalTagAttribute)
						// This is TAL command attribute
						expression = FormatExpression(att.Value);
					else
					{
						// This is clean attribute (no TAL command)
						if (_currentScope.Interpolation)
							// Interpolate attribute value
							expression = FormatStringExpression(att.Value);
						else
							// Get attribute value as raw string
							expression = string.Format(@"@""{0}""", att.Value.Replace(@"""", @""""""));
					}
					WriteToBody(@"try");
					WriteToBody(@"{{");
					WriteToBody(@"    attribute_{0}_{1} = {2};", attVarName, scopeId, expression);
					WriteToBody(@"}}");
					WriteToBody(@"catch (Exception ex)");
					WriteToBody(@"{{");
					WriteToBody(@"    attribute_{0}_{1} = null;", attVarName, scopeId);
					WriteToBody(@"}}");
					WriteToBody(@"if (attribute_{0}_{1} == null)", attVarName, scopeId);
					WriteToBody(@"    __currentAttributes.Remove(""{0}"");", att.Name);
					WriteToBody(@"else if (!IsDefaultValue(attribute_{0}_{1}))", attVarName, scopeId);
					WriteToBody(@"{{");
					WriteToBody(@"    if (!__currentAttributes.ContainsKey(""{0}""))", att.Name);
					WriteToBody(@"        __currentAttributes.Add(""{0}"", new Attr {{ Name = @""{0}"", Value = @"""", Eq = @""{1}"", Quote = @""{2}"", QuoteEntity = @""{3}"" }});",
						att.Name, att.Eq, att.Quote.Replace(@"""", @""""""), att.QuoteEntity);
					WriteToBody(@"    __currentAttributes[""{0}""].Value = FormatResult(attribute_{1}_{2});", att.Name, attVarName, scopeId);
					WriteToBody(@"}}");
				}
			}
		}

		protected override void Handle_CMD_START_TAG(ICommand command)
		{
			var startTagCmd = (CmdStartTag)command;

			string tagName = startTagCmd.Tag.Name;
			string tagSuffix = startTagCmd.Tag.Suffix;

			string scopeId = _currentScope.Id;

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
			WriteToBody(@"    goto TAL_ENDTAG_ENDSCOPE_{0};", scopeId);
			WriteToBody(@"}}");
		}

		protected override void Handle_CMD_ENDTAG_ENDSCOPE(ICommand command)
		{
			var entTagEndScopeCmd = (CmdEntTagEndScope)command;

			string tagName = entTagEndScopeCmd.Tag.Name;
			bool singletonTag = entTagEndScopeCmd.Tag.Singleton;

			WriteCmdInfo(command);

			string scopeId = _currentScope.Id;

			WriteToBody("TAL_ENDTAG_ENDSCOPE_{0}:", scopeId);
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
			WriteToBody(@"        output.Write(Escape(FormatResult(__tagContent)));");
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

			while (_currentScope.SubScopeStack.Count > 0)
			{
				SubScope subScope = _currentScope.SubScopeStack.Pop();
				var scope = subScope as RepeatScope;
				if (scope != null)
				{
					RepeatScope repeatScope = scope;
					WriteToBody("");
					WriteToBody("// End sub-scope repeat: {0}", scope.Id);
					WriteToBody(@"");
					WriteToBody(@"// Restore the current attributes");
					WriteToBody(@"__currentAttributes = new Dictionary<string, Attr>(__currentAttributesCopy_{1});", scopeId, repeatScope.Id);
					WriteToBody(@"");
					_rendererBodyTabs = _rendererBodyTabs.Remove(_rendererBodyTabs.Length - 5, 4);
					WriteToBody("}}");
					WriteToBody(@"while (!islast_{0}_{1});", repeatScope.VarName, repeatScope.Id);
					WriteToBody(@"END_REPEAT_{0}:", repeatScope.Id);
					WriteToBody(@"repeat[""{0}""] = null;", repeatScope.VarName);
				}
				else
				{
					WriteToBody("");
					WriteToBody("// End sub-scope: {0}", subScope.Id);
					_rendererBodyTabs = _rendererBodyTabs.Remove(_rendererBodyTabs.Length - 5, 4);
					WriteToBody("}}");
				}
			}

			WriteToBody("");

			WriteToBody(@"List<object> pop_scope_{0} = __scopeStack.Pop();", scopeId);
			WriteToBody(@"__moveToEndTag = (bool)pop_scope_{0}[0];", scopeId);
			WriteToBody(@"__outputTag = (int)pop_scope_{0}[1];", scopeId);
			WriteToBody(@"__currentAttributes = (Dictionary<string, Attr>)pop_scope_{0}[2];", scopeId);
			WriteToBody(@"__tagContent = (object)pop_scope_{0}[3];", scopeId);
			WriteToBody(@"__tagContentType = (int)pop_scope_{0}[4];", scopeId);

			WriteToBody("");
			WriteToBody("// End scope: {0}", scopeId);
			_rendererBodyTabs = _rendererBodyTabs.Remove(_rendererBodyTabs.Length - 5, 4);
			WriteToBody("}}");

			_currentScope = _scopeStack.Pop();
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
