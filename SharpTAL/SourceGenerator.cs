//
// SourceGenerator.cs
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
using System.Text;

namespace SharpTAL
{
    class SourceGenerator
    {
        protected static readonly string FileBodyTemplate =
            @"${usings}

namespace Templates
{
    [SecurityPermission(SecurityAction.PermitOnly, Execution = true)]
    public class Template_${template_hash}
    {
        public static void Render(StreamWriter output, Dictionary<string, object> globals)
        {
            Stack<List<object>> __programStack = new Stack<List<object>>();
            Stack<List<object>> __scopeStack = new Stack<List<object>>();
            bool __moveToEndTag = false;
            int __outputTag = 1;
            object __tagContent = null;
            int __tagContentType = 1;
            Dictionary<string, string> __macros = new Dictionary<string, string>();
            Dictionary<string, string> __originalAttributes = new Dictionary<string, string>();
            Dictionary<string, string> __currentAttributes = new Dictionary<string, string>();
            Dictionary<string, string> __repeatAttributesCopy = new Dictionary<string, string>();
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
                    __originalAttributes = new Dictionary<string, string>();
                    __currentAttributes = new Dictionary<string, string>();
                    __currentCmdInfo = null;
                    // Used in repeat only.
                    __repeatAttributesCopy = new Dictionary<string, string>();
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
                        __originalAttributes,
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
                    __originalAttributes = (Dictionary<string, string>)vars[8];
                    __currentAttributes = (Dictionary<string, string>)vars[9];
                    __repeatAttributesCopy = (Dictionary<string, string>)vars[10];
                    __currentCmdInfo = (CommandInfo)vars[11];
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
        
        private delegate void MacroDelegate();
        
        // Global constants
        private const string DEFAULT_VALUE = ""${defaultvalue}"";
        
        private class CompiledTemplate
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
        
        private static string EscapeXml(string s)
        {
            return EscapeXml(s, false);
        }
        
        private static string EscapeXml(string s, bool quote)
        {
            string xml = s;
            if (!string.IsNullOrEmpty(xml))
            {
                xml = xml.Replace(""&"", ""&amp;"");
                xml = xml.Replace(""<"", ""&lt;"");
                xml = xml.Replace("">"", ""&gt;"");
                if (quote)
                    xml = xml.Replace(""\"""", ""&quot;"");
            }
            return xml;
        }
        
        private static string TagAsText(string tagName, Dictionary<string, string> tagAttributes, int singletonFlag)
        {
            string result = ""<"";
            result += tagName;
            foreach (KeyValuePair<string, string> att in tagAttributes)
            {
                result += "" "";
                result += att.Key;
                result += ""=\"""";
                result += EscapeXml(att.Value, true);
                result += ""\"""";
            }
            if (singletonFlag == 1)
                result += "" />"";
            else
                result += "">"";
            return result;
        }
        
        private static string FormatResult(object result)
        {
            string resultValue = """";
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
        
        //
        // TALES support classes
        //
        
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
        protected class Scope
        {
            public string ID;
            public Stack<SubScope> SubScope = new Stack<SubScope>();
        }

        protected class SubScope
        {
            public string ID;
        }

        protected class RepeatScope : SubScope
        {
            public string VarName;
        }

        protected delegate void ExecuteCommandDelegate(TALCommand command);

        protected List<string> m_GlobalNames;
        protected Dictionary<string, string> m_GlobalsTypeNames;
        protected StringBuilder m_GlobalsBody;
        protected string m_GlobalsBodyTabs;
        protected StringBuilder m_RendererBody;
        protected string m_RendererBodyTabs;
        protected Scope m_CurrentScope;
        protected Stack<Scope> m_ScopeStack;
        protected Dictionary<int, ExecuteCommandDelegate> commandHandler;

        public SourceGenerator()
        {
            m_GlobalNames = new List<string>();
            m_GlobalsTypeNames = new Dictionary<string, string>();
            m_GlobalsBody = new StringBuilder();
            m_GlobalsBodyTabs = "                ";
            m_RendererBody = new StringBuilder();
            m_RendererBodyTabs = "                ";
            m_ScopeStack = new Stack<Scope>();

            this.commandHandler = new Dictionary<int, ExecuteCommandDelegate>();
            this.commandHandler.Add(Constants.TAL_DEFINE, this.Handle_TAL_DEFINE);
            this.commandHandler.Add(Constants.TAL_CONDITION, this.Handle_TAL_CONDITION);
            this.commandHandler.Add(Constants.TAL_REPEAT, this.Handle_TAL_REPEAT);
            this.commandHandler.Add(Constants.TAL_CONTENT, this.Handle_TAL_CONTENT);
            this.commandHandler.Add(Constants.TAL_ATTRIBUTES, this.Handle_TAL_ATTRIBUTES);
            this.commandHandler.Add(Constants.TAL_OMITTAG, this.Handle_TAL_OMITTAG);
            this.commandHandler.Add(Constants.TAL_START_SCOPE, this.Handle_TAL_START_SCOPE);
            this.commandHandler.Add(Constants.TAL_OUTPUT, this.Handle_TAL_OUTPUT);
            this.commandHandler.Add(Constants.TAL_STARTTAG, this.Handle_TAL_STARTTAG);
            this.commandHandler.Add(Constants.TAL_ENDTAG_ENDSCOPE, this.Handle_TAL_ENDTAG_ENDSCOPE);
            this.commandHandler.Add(Constants.TAL_NOOP, this.Handle_TAL_NOOP);
            this.commandHandler.Add(Constants.METAL_USE_MACRO, this.Handle_METAL_USE_MACRO);
            this.commandHandler.Add(Constants.METAL_DEFINE_SLOT, this.Handle_METAL_DEFINE_SLOT);
            this.commandHandler.Add(Constants.METAL_DEFINE_PARAM, this.Handle_METAL_DEFINE_PARAM);
        }

        public string GenerateSource(TemplateInfo ti)
        {
            m_ScopeStack = new Stack<Scope>();

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
                        string typeName = this.ResolveTypeName(type);
                        m_GlobalNames.Add(varName);
                        this.WriteToGlobals(@"{0} {1} = ({0})globals[""{1}""];", typeName, varName);
                    }
                }
            }

            //-----------------------
            // Process macro commands
            //-----------------------

            List<string> processedTemplateNames = new List<string>();

            // Process macro commands in main template and inline templates
            foreach (string templateName in ti.Programs.Keys)
            {
                TALProgram talProgram = ti.Programs[templateName];
                ProcessMacroCommands(talProgram, templateName, "", processedTemplateNames);
            }

            // Process macro commands in imported templates
            foreach (string destNs in ti.ImportedNamespaces.Keys)
            {
                foreach (string sourcePath in ti.ImportedNamespaces[destNs])
                {
                    TALProgram talProgram = ti.ImportedPrograms[sourcePath];

                    ProcessMacroCommands(talProgram, destNs, sourcePath, processedTemplateNames);
                }
            }

            // Main template macros are also in the global namespace
            this.WriteToGlobals(@"macros = {0}.macros;", "template");

            //-------------------------------------------------------------
            // Process main template commands (ignoring macro commands)
            //-------------------------------------------------------------

            TALProgram mainTALProgram = ti.Programs["template"];

            this.WriteToBody(@"//====================");
            this.WriteToBody(@"// Template:");
            this.WriteToBody(@"//====================");
            this.WriteToBody(@"");
            this.WriteToBody(@"__CleanProgram();");
            this.WriteToBody(@"");
            List<object> template_prog = mainTALProgram.GetProgram();
            List<TALCommand> template_commandList = (List<TALCommand>)template_prog[0];
            int template_programStart = (int)template_prog[1];
            int template_programCounter = template_programStart;
            int template_programLength = (int)template_prog[2];
            while (template_programCounter < template_programLength)
            {
                // Ignore macro commands
                bool isMacroCommand = IsInsideMacro(template_programStart, template_programLength, template_programCounter,
                    mainTALProgram.Macros, null);
                if (isMacroCommand)
                {
                    template_programCounter += 1;
                    continue;
                }

                TALCommand template_cmd = template_commandList[template_programCounter];
                this.commandHandler[template_cmd.ID](template_cmd);
                template_programCounter++;
            }

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
				"System.Collections",
				"System.Collections.Generic",
				"System.Security.Permissions",
				"System.Security"
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
                            GetExtMethodsNs(namespacesList, type.Assembly);
                            assemblies.Add(type.Assembly.Location);

                            // Referenced assemblies
                            foreach (AssemblyName assemblyName in type.Assembly.GetReferencedAssemblies())
                            {
                                Assembly assembly = AppDomain.CurrentDomain.Load(assemblyName);
                                if (!assemblies.Contains(assembly.Location) &&
                                    !assemblies.Contains(Path.GetFileName(assembly.Location)))
                                {
                                    GetExtMethodsNs(namespacesList, assembly);
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
                        GetExtMethodsNs(namespacesList, refAsm);
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
                Replace("${defaultvalue}", Constants.DEFAULTVALUE).
                Replace("${usings}", usings).
                Replace("${template_hash}", ti.TemplateHash).
                Replace("${globals}", m_GlobalsBody.ToString()).
                Replace("${body}", m_RendererBody.ToString());
            return templateSource;
        }

        private void ProcessMacroCommands(TALProgram talProgram, string templateName, string sourcePath,
            List<string> processedTemplateNames)
        {
            if (talProgram.Macros != null)
            {
                if (!processedTemplateNames.Contains(templateName))
                {
                    // Check if other global var is defined with the name as this template
                    if (m_GlobalNames.Contains(templateName))
                    {
                        throw new InvalidOperationException(string.Format(@"Failed to process macros in namespace ""{0}"".
Global variable with the same name allready exists.", templateName));
                    }
                    m_GlobalNames.Add(templateName);
                    processedTemplateNames.Add(templateName);
                    this.WriteToGlobals(@"CompiledTemplate {0} = new CompiledTemplate();", templateName);
                }

                foreach (string macroName in talProgram.Macros.Keys)
                {
                    TALSubProgram macro = talProgram.Macros[macroName];

                    // Create macro delegate
                    this.WriteToBody(@"//====================");
                    this.WriteToBody(@"// Macro: ""{0}.{1}""", templateName, macroName);
                    this.WriteToBody(@"// Source: ""{0}""", sourcePath);
                    this.WriteToBody(@"//====================");
                    this.WriteToBody(@"MacroDelegate macro_{0}_{1} = delegate()", templateName, macroName);
                    this.WriteToBody(@"{{");
                    m_RendererBodyTabs += "    ";
                    this.WriteToBody(@"__CleanProgram();");

                    // Process macro commands
                    List<object> macro_prog = macro.GetProgram();
                    List<TALCommand> macro_commandList = (List<TALCommand>)macro_prog[0];
                    int macro_programStart = (int)macro_prog[1];
                    int macro_programCounter = macro_programStart;
                    int macro_programLength = (int)macro_prog[2];

                    // Process METAL_DEFINE_PARAM commands
                    while (macro_programCounter < macro_programLength)
                    {
                        // Check if the command is inside sub macro
                        bool isMacroCommand = IsInsideMacro(macro_programStart, macro_programLength, macro_programCounter,
                            talProgram.Macros, macroName);
                        if (isMacroCommand)
                        {
                            macro_programCounter += 1;
                            continue;
                        }
                        TALCommand macro_cmd = macro_commandList[macro_programCounter];
                        if (macro_cmd.ID == Constants.METAL_DEFINE_PARAM)
                        {
                            this.commandHandler[macro_cmd.ID](macro_cmd);
                        }
                        macro_programCounter++;
                    }

                    // Process macro commands and ignore METAL_DEFINE_PARAM commands
                    macro_programCounter = (int)macro_prog[1];
                    while (macro_programCounter < macro_programLength)
                    {
                        // Check if the command is inside sub macro
                        bool isMacroCommand = IsInsideMacro(macro_programStart, macro_programLength, macro_programCounter,
                            talProgram.Macros, macroName);
                        if (isMacroCommand)
                        {
                            macro_programCounter += 1;
                            continue;
                        }
                        TALCommand macro_cmd = macro_commandList[macro_programCounter];
                        if (macro_cmd.ID != Constants.METAL_DEFINE_PARAM)
                        {
                            this.commandHandler[macro_cmd.ID](macro_cmd);
                        }
                        macro_programCounter++;
                    }

                    // Finalize macro delegate
                    m_RendererBodyTabs = m_RendererBodyTabs.Remove(m_RendererBodyTabs.Length - 5, 4);
                    this.WriteToBody(@"}};");
                    this.WriteToBody(@"{0}.macros.Add(""{1}"", macro_{0}_{1});", templateName, macroName);
                    this.WriteToBody(@"__macros.Add(@""{0}.{1}"", @""{2}"");", templateName, macroName, sourcePath);
                    this.WriteToBody(@"");
                }
            }
        }

        private bool IsInsideMacro(int programStart, int programLength, int programCounter,
            Dictionary<string, TALSubProgram> macros, string mainMacro)
        {
            // Check if the command is inside sub macro
            bool isMacroCommand = false;
            foreach (string macroName in macros.Keys)
            {
                if (string.IsNullOrEmpty(mainMacro) || macroName != mainMacro)
                {
                    TALSubProgram macro = macros[macroName];
                    List<object> macro_prog = macro.GetProgram();
                    int macro_programStart = (int)macro_prog[1];
                    int macro_programLength = (int)macro_prog[2];
                    // Macro program starts after and ends before this program
                    if (programStart < macro_programStart &&
                        (programStart + programLength) > (macro_programStart + macro_programLength) &&
                        // Instruction is inside the macro
                        programCounter >= macro_programStart &&
                        programCounter < macro_programLength)
                    {
                        isMacroCommand = true;
                        break;
                    }
                }
            }
            return isMacroCommand;
        }

        private static void GetExtMethodsNs(List<string> namespacesList, Assembly assembly)
        {
            if (assembly.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
            {
                foreach (Type tp in assembly.GetTypes())
                {
                    // Check if type has defined "ExtensionAttribute"
                    if (tp.IsSealed && !tp.IsGenericType && !tp.IsNested &&
                        tp.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
                    {
                        if (!namespacesList.Contains(tp.Namespace))
                        {
                            namespacesList.Add(tp.Namespace);
                        }
                    }
                }
            }
        }

        protected void Handle_TAL_DEFINE(TALCommand command)
        {
            // args: [(DefineAction (global, local, set), variableName, variablePath),...]
            //        Define variables in either the local or global context
            List<DefineInfo> args = (List<DefineInfo>)command.Attributes[0];

            WriteCmdInfo(command);

            foreach (DefineInfo di in args)
            {
                string expression = this.FormatExpression(di.varPath);
                if (di.defAction == DefineAction.Local)
                {
                    // Create new local variable
                    string body = string.Format(@"var {0} = {1};", di.varName, expression);
                    this.WriteToBody(body);
                }
                else if (di.defAction == DefineAction.SetLocal)
                {
                    // Set existing local variable
                    string body = string.Format(@"{0} = {1};", di.varName, expression);
                    this.WriteToBody(body);
                }
                else
                {
                    if (m_GlobalNames.Contains(di.varName))
                    {
                        // Set existing global variable
                        string body = string.Format(@"{0} = {1};", di.varName, expression);
                        this.WriteToBody(body);
                    }
                    else
                    {
                        // Create new global variable
                        string body = string.Format(@"var {0} = {1};", di.varName, expression);
                        m_GlobalNames.Add(di.varName);
                        this.WriteToGlobals(body);
                    }
                }
            }
        }

        protected void Handle_TAL_CONDITION(TALCommand command)
        {
            // args: expression, endTagSymbol
            //        Conditionally continues with execution of all content contained
            //        by it.
            string expression = (string)command.Attributes[0];

            string scopeID = m_CurrentScope.ID;

            WriteCmdInfo(command);

            // Start SubScope
            expression = this.FormatExpression(expression);
            this.WriteToBody(@"if (IsFalseResult({0}))", expression);
            this.WriteToBody(@"{{");
            this.WriteToBody(@"    // Nothing to output - evaluated to false.");
            this.WriteToBody(@"    __outputTag = 0;");
            this.WriteToBody(@"    __tagContent = null;");
            this.WriteToBody(@"    goto TAL_ENDTAG_ENDSCOPE_{0};", scopeID);
            this.WriteToBody(@"}}");
        }

        protected void Handle_TAL_REPEAT(TALCommand command)
        {
            // args: (varName, expression, endTagSymbol)
            //        Repeats anything in the cmndList
            string varName = (string)command.Attributes[0];
            string expression = (string)command.Attributes[1];

            // Start Repeat SubScope
            string repeatSubScopeID = Guid.NewGuid().ToString().Replace("-", "");
            m_CurrentScope.SubScope.Push(new RepeatScope() { ID = repeatSubScopeID, VarName = varName });

            WriteCmdInfo(command);

            expression = this.FormatExpression(expression);

            this.WriteToBody(@"// Backup the current attributes for this tag");
            this.WriteToBody(@"Dictionary<string, string> __currentAttributesCopy_{0} = new Dictionary<string, string>(__currentAttributes);", repeatSubScopeID);
            this.WriteToBody(@"");
            this.WriteToBody(@"var repeat_expression_{0} = {1};", repeatSubScopeID, expression);
            this.WriteToBody(@"var enumerable_{0}_{1} = repeat_expression_{1};", varName, repeatSubScopeID);
            this.WriteToBody(@"var enumerator_{0}_{1} = enumerable_{0}_{1}.GetEnumerator();", varName, repeatSubScopeID);
            this.WriteToBody(@"bool enumerator_status_{0}_{1} = enumerator_{0}_{1}.MoveNext();", varName, repeatSubScopeID);
            this.WriteToBody(@"bool enumerator_isdefault_{0}_{1} = false;", varName, repeatSubScopeID);
            this.WriteToBody(@"bool enumerator_isfirst_{0}_{1} = true;", varName, repeatSubScopeID);
            this.WriteToBody(@"if (IsDefaultValue(repeat_expression_{0}))", repeatSubScopeID);
            this.WriteToBody(@"{{");
            this.WriteToBody(@"    // Stop after first enumeration, so only default content is rendered");
            this.WriteToBody(@"    enumerator_status_{0}_{1} = false;", varName, repeatSubScopeID);
            this.WriteToBody(@"    enumerator_isdefault_{0}_{1} = true;", varName, repeatSubScopeID);
            this.WriteToBody(@"}}");
            this.WriteToBody(@"else");
            this.WriteToBody(@"{{");
            this.WriteToBody(@"    repeat[""{0}""] = new RepeatVariable(enumerable_{0}_{1});", varName, repeatSubScopeID);
            this.WriteToBody(@"}}");
            this.WriteToBody(@"do");
            this.WriteToBody(@"{{");
            this.WriteToBody(@"    __outputTag = 1;");
            this.WriteToBody(@"    __tagContent = null;");
            this.WriteToBody(@"    __tagContentType = 0;");
            this.WriteToBody(@"    __moveToEndTag = false;");
            this.WriteToBody(@"    ");
            this.WriteToBody(@"    // Skip repeat, if there is nothing to enumerate");
            this.WriteToBody(@"    if (enumerator_status_{0}_{1} == false &&", varName, repeatSubScopeID);
            this.WriteToBody(@"        enumerator_isfirst_{0}_{1} == true &&", varName, repeatSubScopeID);
            this.WriteToBody(@"        enumerator_isdefault_{0}_{1} == false)", varName, repeatSubScopeID);
            this.WriteToBody(@"    {{");
            this.WriteToBody(@"        goto END_REPEAT_{0};", repeatSubScopeID);
            this.WriteToBody(@"    }}");
            this.WriteToBody(@"    enumerator_isfirst_{0}_{1} = false;", varName, repeatSubScopeID);
            this.WriteToBody(@"    ");
            this.WriteToBody(@"    var {0} = enumerator_{0}_{1}.Current;", varName, repeatSubScopeID);
            this.WriteToBody(@"    if (!enumerator_isdefault_{0}_{1})", varName, repeatSubScopeID);
            this.WriteToBody(@"    {{");
            this.WriteToBody(@"        enumerator_status_{0}_{1} = enumerator_{0}_{1}.MoveNext();", varName, repeatSubScopeID);
            this.WriteToBody(@"        repeat[""{0}""].UpdateStatus(enumerator_status_{0}_{1});", varName, repeatSubScopeID);
            this.WriteToBody(@"    }}");

            m_RendererBodyTabs += "    ";
        }

        protected void Handle_TAL_CONTENT(TALCommand command)
        {
            // args: (replaceFlag, structureFlag, expression, endTagSymbol)
            //        Expands content
            int replaceFlag = (int)command.Attributes[0];
            int structureFlag = (int)command.Attributes[1];
            string expression = (string)command.Attributes[2];

            string scopeID = m_CurrentScope.ID;

            WriteCmdInfo(command);

            expression = this.FormatExpression(expression);
            this.WriteToBody(@"object content_expression_result_{0} = {1};", scopeID, expression);
            this.WriteToBody(@"");
            this.WriteToBody(@"if (content_expression_result_{0} == null)", scopeID);
            this.WriteToBody(@"{{");
            this.WriteToBody(@"    if ({0} == 1)", replaceFlag);
            this.WriteToBody(@"    {{");
            this.WriteToBody(@"        // Only output tags if this is a content not a replace");
            this.WriteToBody(@"        __outputTag = 0;");
            this.WriteToBody(@"    }}");
            this.WriteToBody(@"    // Output none of our content or the existing content, but potentially the tags");
            this.WriteToBody(@"    __moveToEndTag = true;", scopeID);
            this.WriteToBody(@"}}");
            this.WriteToBody(@"else if (!IsDefaultValue(content_expression_result_{0}))", scopeID);
            this.WriteToBody(@"{{");
            this.WriteToBody(@"    // We have content, so let's suppress the natural content and output this!");
            this.WriteToBody(@"    if ({0} == 1)", replaceFlag);
            this.WriteToBody(@"    {{");
            this.WriteToBody(@"        // Replace content - do not output tags");
            this.WriteToBody(@"        __outputTag = 0;");
            this.WriteToBody(@"    }}");
            this.WriteToBody(@"    __tagContent = {0};", expression);
            this.WriteToBody(@"    __tagContentType = {0};", structureFlag);
            this.WriteToBody(@"    __moveToEndTag = true;", scopeID);
            this.WriteToBody(@"}}");
        }

        protected void Handle_TAL_ATTRIBUTES(TALCommand command)
        {
            // args: [(attributeName, expression)]
            //        Add, leave, or remove attributes from the start tag
            Dictionary<string, string> args = (Dictionary<string, string>)command.Attributes[0];

            string scopeID = m_CurrentScope.ID;

            WriteCmdInfo(command);

            foreach (KeyValuePair<string, string> kw in args)
            {
                string attName = kw.Key;
                string attExpr = kw.Value;
                string expression = this.FormatExpression(attExpr);

                // Eval Expression
                this.WriteToBody(@"object attribute_{0}_{1} = null;", attName, scopeID);
                this.WriteToBody(@"try");
                this.WriteToBody(@"{{");
                this.WriteToBody(@"    attribute_{0}_{1} = {2};", attName, scopeID, expression);
                this.WriteToBody(@"}}");
                this.WriteToBody(@"catch (Exception ex)");
                this.WriteToBody(@"{{");
                this.WriteToBody(@"    attribute_{0}_{1} = null;", attName, scopeID);
                this.WriteToBody(@"}}");

                this.WriteToBody(@"if (attribute_{0}_{1} == null)", attName, scopeID);
                this.WriteToBody(@"{{");
                this.WriteToBody(@"    __currentAttributes.Remove(""{1}"");", scopeID, attName);
                this.WriteToBody(@"}}");
                this.WriteToBody(@"else if (!IsDefaultValue(attribute_{0}_{1}))", attName, scopeID);
                this.WriteToBody(@"{{");
                this.WriteToBody(@"    __currentAttributes[""{1}""] = FormatResult(attribute_{1}_{0});", scopeID, attName);
                this.WriteToBody(@"}}");
            }
        }

        protected void Handle_TAL_OUTPUT(TALCommand command)
        {
            string data = "";
            foreach (object s in command.Attributes)
            {
                data += (object)s;
            }

            WriteCmdInfo(command);

            this.WriteToBody(@"output.Write(@""{0}"");", data.Replace(@"""", @""""""));
        }

        protected void Handle_TAL_OMITTAG(TALCommand command)
        {
            // Ignore this command in tag wihout the scope
            if (command.Tag.OmitTagScope)
            {
                return;
            }

            // args: expression
            //       Conditionally turn off tag output
            string expression = (string)command.Attributes[0];

            WriteCmdInfo(command);

            expression = this.FormatExpression(expression);
            this.WriteToBody(@"if (!IsFalseResult({0}))", expression);
            this.WriteToBody(@"{{");
            this.WriteToBody(@"    // Turn tag output off");
            this.WriteToBody(@"    __outputTag = 0;");
            this.WriteToBody(@"}}");
        }

        protected void Handle_TAL_STARTTAG(TALCommand command)
        {
            // Ignore this command in tag wihout the scope
            if (command.Tag.OmitTagScope)
            {
                return;
            }

            // Args: tagName
            Tag tag = (Tag)command.Attributes[0];
            string tagName = tag.Name;
            int singletonTag = (int)command.Attributes[1];

            string scopeID = m_CurrentScope.ID;

            WriteCmdInfo(command);

            this.WriteToBody(@"if (__outputTag == 1)");
            this.WriteToBody(@"{{");
            this.WriteToBody(@"    if ((__tagContent == null && {0} == 1))", singletonTag);
            this.WriteToBody(@"    {{");
            this.WriteToBody(@"        output.Write(TagAsText(""{0}"", __currentAttributes, {1}));", tagName, singletonTag);
            this.WriteToBody(@"    }}");
            this.WriteToBody(@"    else");
            this.WriteToBody(@"    {{");
            this.WriteToBody(@"        output.Write(TagAsText(""{0}"", __currentAttributes, 0));", tagName);
            this.WriteToBody(@"    }}");
            this.WriteToBody(@"}}");
            this.WriteToBody(@"");
            this.WriteToBody(@"if (__moveToEndTag == true)");
            this.WriteToBody(@"{{");
            this.WriteToBody(@"    goto TAL_ENDTAG_ENDSCOPE_{0};", scopeID);
            this.WriteToBody(@"}}");
        }

        protected void Handle_TAL_START_SCOPE(TALCommand command)
        {
            // Ignore this command in tag wihout the scope
            if (command.Tag.OmitTagScope)
            {
                return;
            }

            // args: (originalAttributes, currentAttributes)
            //        Pushes the current state onto the stack, and sets up the new state
            Dictionary<string, string> originalAttributes = (Dictionary<string, string>)command.Attributes[0];
            Dictionary<string, string> currentAttributes = (Dictionary<string, string>)command.Attributes[1];

            WriteCmdInfo(command);

            m_ScopeStack.Push(m_CurrentScope);
            m_CurrentScope = new Scope()
            {
                ID = Guid.NewGuid().ToString().Replace("-", ""),
                SubScope = new Stack<SubScope>()
            };
            this.WriteToBody("");
            this.WriteToBody("// Start scope: {0}", m_CurrentScope.ID);
            this.WriteToBody("{{");
            m_RendererBodyTabs += "    ";

            string scopeID = m_CurrentScope.ID;

            this.WriteToBody("");
            this.WriteToBody("// Original attributes:");
            this.WriteToBody(@"Dictionary<string, string> __originalAttributes_{0} = new Dictionary<string, string>();", scopeID);
            foreach (KeyValuePair<string, string> kw in originalAttributes)
            {
                this.WriteToBody(@"__originalAttributes_{0}.Add(""{1}"", @""{2}"");", scopeID, kw.Key, kw.Value.Replace(@"""", @""""""));
            }
            this.WriteToBody("");
            this.WriteToBody("// Current attributes:");
            this.WriteToBody(@"Dictionary<string, string> __currentAttributes_{0} = new Dictionary<string, string>();", scopeID);
            foreach (KeyValuePair<string, string> kw in currentAttributes)
            {
                this.WriteToBody(@"__currentAttributes_{0}.Add(""{1}"", @""{2}"");", scopeID, kw.Key, kw.Value.Replace(@"""", @""""""));
            }

            this.WriteToBody(@"");
            this.WriteToBody(@"List<object> push_scope_{0} = new List<object>()", scopeID);
            this.WriteToBody(@"{{");
            this.WriteToBody(@"    __moveToEndTag,");
            this.WriteToBody(@"    __outputTag,");
            this.WriteToBody(@"    __originalAttributes,");
            this.WriteToBody(@"    __currentAttributes,");
            this.WriteToBody(@"    __tagContent,");
            this.WriteToBody(@"    __tagContentType");
            this.WriteToBody(@"}};");
            this.WriteToBody(@"");
            this.WriteToBody(@"__scopeStack.Push(push_scope_{0});", scopeID);
            this.WriteToBody(@"");
            this.WriteToBody(@"__moveToEndTag = false;");
            this.WriteToBody(@"__outputTag = 1;");
            this.WriteToBody(@"__originalAttributes = __originalAttributes_{0};", scopeID);
            this.WriteToBody(@"__currentAttributes = __currentAttributes_{0};", scopeID);
            this.WriteToBody(@"__tagContent = null;");
            this.WriteToBody(@"__tagContentType = 1;");
        }

        protected void Handle_TAL_ENDTAG_ENDSCOPE(TALCommand command)
        {
            // Ignore this command in tag wihout the scope
            if (command.Tag.OmitTagScope)
            {
                return;
            }

            // Args: tagName, omitFlag, singletonTag
            string tagName = (string)command.Attributes[0];
            int omitFlag = (int)command.Attributes[1];
            int singletonTag = (int)command.Attributes[2];

            WriteCmdInfo(command);

            string scopeID = m_CurrentScope.ID;

            this.WriteToBody("TAL_ENDTAG_ENDSCOPE_{0}:", scopeID);
            this.WriteToBody("");
            this.WriteToBody("// End tag: <{0}>", tagName);

            this.WriteToBody(@"if (__tagContent != null)");
            this.WriteToBody(@"{{");
            this.WriteToBody(@"    if (__tagContentType == 1)");
            this.WriteToBody(@"    {{");
            this.WriteToBody(@"        if (__tagContent is MacroDelegate)");
            this.WriteToBody(@"        {{");
            this.WriteToBody(@"            // Save our state!");
            this.WriteToBody(@"            __PushProgram();");
            this.WriteToBody(@"            // Execute macro or slot delegate");
            this.WriteToBody(@"            ((MacroDelegate)__tagContent)();");
            this.WriteToBody(@"            // Restore state");
            this.WriteToBody(@"            __PopProgram();");
            this.WriteToBody(@"            // End of the macro expansion (if any) so clear the slots and params");
            this.WriteToBody(@"            __slotMap = new Dictionary<string, MacroDelegate>();");
            this.WriteToBody(@"            __paramMap = new Dictionary<string, object>();");
            this.WriteToBody(@"        }}");
            this.WriteToBody(@"        else");
            this.WriteToBody(@"        {{");
            this.WriteToBody(@"            output.Write((string)__tagContent);");
            this.WriteToBody(@"        }}");
            this.WriteToBody(@"    }}");
            this.WriteToBody(@"    else");
            this.WriteToBody(@"    {{");
            this.WriteToBody(@"        output.Write(EscapeXml(FormatResult(__tagContent)));");
            this.WriteToBody(@"    }}");
            this.WriteToBody(@"}}");

            this.WriteToBody(@"if (__outputTag == 1 && {0} == 0)", omitFlag);
            this.WriteToBody(@"{{");
            this.WriteToBody(@"    // Do NOT output end tag if a singleton with no content");
            this.WriteToBody(@"    if (({0} == 1 && __tagContent == null) == false)", singletonTag);
            this.WriteToBody(@"    {{");
            this.WriteToBody(@"        output.Write(""</{0}>"");", tagName);
            this.WriteToBody(@"    }}");
            this.WriteToBody(@"}}");

            while (m_CurrentScope.SubScope.Count > 0)
            {
                SubScope subScope = m_CurrentScope.SubScope.Pop();
                if (subScope is RepeatScope)
                {
                    this.WriteToBody("");
                    this.WriteToBody("// End sub-scope repeat: {0}", subScope.ID);
                    this.WriteToBody(@"");
                    this.WriteToBody(@"// Restore the current attributes");
                    this.WriteToBody(@"__currentAttributes = new Dictionary<string, string>(__currentAttributesCopy_{1});", scopeID, subScope.ID);
                    this.WriteToBody(@"");
                    m_RendererBodyTabs = m_RendererBodyTabs.Remove(m_RendererBodyTabs.Length - 5, 4);
                    this.WriteToBody("}}");
                    this.WriteToBody(@"while (enumerator_status_{0}_{1} == true);", ((RepeatScope)subScope).VarName, subScope.ID);
                    this.WriteToBody(@"END_REPEAT_{0}:", subScope.ID);
                    this.WriteToBody(@"repeat[""{0}""] = null;", ((RepeatScope)subScope).VarName);
                }
                else
                {
                    this.WriteToBody("");
                    this.WriteToBody("// End sub-scope: {0}", subScope.ID);
                    m_RendererBodyTabs = m_RendererBodyTabs.Remove(m_RendererBodyTabs.Length - 5, 4);
                    this.WriteToBody("}}");
                }
            }

            this.WriteToBody("");

            this.WriteToBody(@"List<object> pop_scope_{0} = __scopeStack.Pop();", scopeID);
            this.WriteToBody(@"__moveToEndTag = (bool)pop_scope_{0}[0];", scopeID);
            this.WriteToBody(@"__outputTag = (int)pop_scope_{0}[1];", scopeID);
            this.WriteToBody(@"__originalAttributes = (Dictionary<string, string>)pop_scope_{0}[2];", scopeID);
            this.WriteToBody(@"__currentAttributes = (Dictionary<string, string>)pop_scope_{0}[3];", scopeID);
            this.WriteToBody(@"__tagContent = (object)pop_scope_{0}[4];", scopeID);
            this.WriteToBody(@"__tagContentType = (int)pop_scope_{0}[5];", scopeID);

            this.WriteToBody("");
            this.WriteToBody("// End scope: {0}", scopeID);
            m_RendererBodyTabs = m_RendererBodyTabs.Remove(m_RendererBodyTabs.Length - 5, 4);
            this.WriteToBody("}}");

            m_CurrentScope = m_ScopeStack.Pop();
        }

        protected void Handle_TAL_NOOP(TALCommand command)
        {
            // Just skip this instruction
        }

        protected void Handle_METAL_USE_MACRO(TALCommand command)
        {
            // args: (macroExpression, slotMap, paramsMap, endTagSymbol)
            //        Evaluates the expression, if it resolves to a SubTemplate it then places
            //        the slotMap into currentSlots and then jumps to the end tag
            string macroExpression = (string)command.Attributes[0];
            Dictionary<string, TALSubProgram> slotMap = (Dictionary<string, TALSubProgram>)command.Attributes[1];
            List<DefineInfo> paramMap = (List<DefineInfo>)command.Attributes[2];

            string scopeID = m_CurrentScope.ID;

            // Start SubScope
            string subScopeID = Guid.NewGuid().ToString().Replace("-", "");
            m_CurrentScope.SubScope.Push(new SubScope() { ID = subScopeID });

            WriteCmdInfo(command);

            string expression = this.FormatExpression(macroExpression);
            this.WriteToBody(@"object use_macro_delegate_{0} = {1};", subScopeID, expression);
            this.WriteToBody(@"if (use_macro_delegate_{0} != null && use_macro_delegate_{0} is MacroDelegate)", subScopeID);
            this.WriteToBody(@"{{");
            m_RendererBodyTabs += "    ";
            this.WriteToBody(@"__outputTag = 0;");
            this.WriteToBody(@"__tagContent = use_macro_delegate_{0};", subScopeID);
            this.WriteToBody(@"__tagContentType = 1;");
            this.WriteToBody(@"__slotMap = new Dictionary<string, MacroDelegate>();");

            // Set macro params
            foreach (DefineInfo di in paramMap)
            {
                this.WriteToBody(@"__paramMap[""{0}""] = {1};", di.varName, this.FormatExpression(di.varPath));
            }

            // Expand slots (SubTemplates)
            foreach (string slotName in slotMap.Keys)
            {
                TALSubProgram slot = slotMap[slotName];

                string slotID = Guid.NewGuid().ToString().Replace("-", "");

                // Create slot delegate
                this.WriteToBody(@"");
                this.WriteToBody(@"//====================");
                this.WriteToBody(@"// Slot: ""{0}""", slotName);
                this.WriteToBody(@"//====================");
                this.WriteToBody(@"MacroDelegate slot_{0}_delegate_{1} = delegate()", slotName, slotID);
                this.WriteToBody(@"{{");
                m_RendererBodyTabs += "    ";

                // Process slot commands
                List<object> slot_prog = slot.GetProgram();
                List<TALCommand> slot_commandList = (List<TALCommand>)slot_prog[0];
                int slot_programCounter = (int)slot_prog[1];
                int slot_programLength = (int)slot_prog[2];
                while (slot_programCounter < slot_programLength)
                {
                    TALCommand slot_cmd = slot_commandList[slot_programCounter];
                    this.commandHandler[slot_cmd.ID](slot_cmd);
                    slot_programCounter++;
                }

                // Finalize slot delegate
                m_RendererBodyTabs = m_RendererBodyTabs.Remove(m_RendererBodyTabs.Length - 5, 4);
                this.WriteToBody(@"}};");
                this.WriteToBody(@"__slotMap[""{0}""] = slot_{0}_delegate_{1};", slotName, slotID);
                this.WriteToBody(@"");
            }

            // Go to end tag
            this.WriteToBody(@"");
            this.WriteToBody(@"// NOTE: WE JUMP STRAIGHT TO THE END TAG, NO OTHER TAL/METAL COMMANDS ARE EVALUATED");
            this.WriteToBody(@"goto TAL_ENDTAG_ENDSCOPE_{0};", scopeID);
        }

        protected void Handle_METAL_DEFINE_SLOT(TALCommand command)
        {
            // args: (slotName, endTagSymbol)
            //        If the slotName is filled then that is used, otherwise the original content
            //        is used.
            string slotName = (string)command.Attributes[0];

            string scopeID = m_CurrentScope.ID;

            WriteCmdInfo(command);

            this.WriteToBody(@"if (__currentSlots.ContainsKey(""{0}""))", slotName);
            this.WriteToBody(@"{{");
            this.WriteToBody("     // This slot is filled, so replace us with that content");
            this.WriteToBody(@"    __outputTag = 0;");
            this.WriteToBody(@"    __tagContent = __currentSlots[""{0}""];", slotName);
            this.WriteToBody(@"    __tagContentType = 1;");
            this.WriteToBody(@"    ");
            this.WriteToBody(@"    // Output none of our content or the existing content");
            this.WriteToBody(@"    // NOTE: NO FURTHER TAL/METAL COMMANDS ARE EVALUATED");
            this.WriteToBody(@"    goto TAL_ENDTAG_ENDSCOPE_{0};", scopeID);
            this.WriteToBody(@"}}");
        }

        protected void Handle_METAL_DEFINE_PARAM(TALCommand command)
        {
            // args: [(paramType, paramName, paramPath),...]
            // Define params in local context of the macro
            List<DefineInfo> args = (List<DefineInfo>)command.Attributes[0];

            WriteCmdInfo(command);

            foreach (DefineInfo di in args)
            {
                // Create param variable
                this.WriteToBody(@"{0} {1} = {2};", di.varType, di.varName, this.FormatExpression(di.varPath));
                this.WriteToBody(@"if (__currentParams.ContainsKey(""{0}""))", di.varName);
                this.WriteToBody(@"{{");
                this.WriteToBody("     // This param is filled");
                this.WriteToBody(@"    {0} = ({1})__currentParams[""{0}""];", di.varName, di.varType);
                this.WriteToBody(@"}}");
            }
        }

        private void WriteCmdInfo(TALCommand command)
        {
            this.WriteToBody("");
            this.WriteToBody("// <CMD_INFO>");
            this.WriteToBody("//  Command:  {0}", Constants.GetCommandName(command.ID));
            if (command.Tag != null)
            {
                this.WriteToBody("//  Tag:        {0}", command.Tag.ToString().Replace(Environment.NewLine, ""));
                this.WriteToBody("//  Line:       {0}", command.Tag.LineNumber);
                this.WriteToBody("//  Position:   {0}", command.Tag.LinePosition);
                this.WriteToBody("//  Source:     {0}", command.Tag.SourcePath);
                this.WriteToBody("//  Omit Scope: {0}", command.Tag.OmitTagScope);
            }
            this.WriteToBody("// </CMD_INFO>");
            this.WriteToBody("__currentCmdInfo = new CommandInfo();");
            this.WriteToBody(@"__currentCmdInfo.CommandName = ""{0}"";", Constants.GetCommandName(command.ID));
            if (command.Tag != null)
            {
                this.WriteToBody(@"__currentCmdInfo.Tag = @""{0}"";", command.Tag.ToString().Replace(Environment.NewLine, "").Replace(@"""", @""""""));
                this.WriteToBody(@"__currentCmdInfo.Line = {0};", command.Tag.LineNumber);
                this.WriteToBody(@"__currentCmdInfo.Position = {0};", command.Tag.LinePosition);
                this.WriteToBody(@"__currentCmdInfo.Source = @""{0}"";", command.Tag.SourcePath);
                this.WriteToBody(@"__currentCmdInfo.OmitTagScope = {0};", command.Tag.OmitTagScope ? "true" : "false");
            }
            this.WriteToBody("");
        }

        protected string FormatExpression(string expression)
        {
            // Expression: "default"
            if (expression.Trim(' ') == "default")
            {
                return "DEFAULT_VALUE";
            }

            // Expression: "string:"
            if (expression.TrimStart(' ').StartsWith("string:"))
            {
                expression = expression.TrimStart(' ').Substring("string:".Length);
                string expressionFormat = "";
                string expressionFormatArguments = "";
                int expressionFormatArgumentsCount = 0;
                int skipCount = 0;
                for (int position = 0; position < expression.Length; position++)
                {
                    if (skipCount > 0)
                    {
                        skipCount -= 1;
                    }
                    else
                    {
                        if (expression[position] == '$')
                        {
                            if (expression.Length > (position + 1) && expression[position + 1] == '$')
                            {
                                // Escaped $ sign
                                expressionFormat += "$";
                                skipCount = 1;
                            }
                            else if (expression.Length > (position + 1) && expression[position + 1] == '{')
                            {
                                // Looking for a path!
                                int endPos = expression.IndexOf("}", position + 1);
                                if (endPos > 0)
                                {
                                    string expr = expression.Substring(position + 2, endPos - (position + 2));
                                    expr = this.FormatExpression(expr);
                                    expressionFormatArguments = string.Format("{0}, {1}", expressionFormatArguments, expr);
                                    expressionFormat = string.Format("{0}{{{1}}}", expressionFormat, expressionFormatArgumentsCount);
                                    expressionFormatArgumentsCount++;
                                    skipCount = endPos - position;
                                }
                            }
                        }
                        else
                        {
                            expressionFormat += expression[position];
                        }
                    }
                }

                expression = string.Format(@"string.Format(""{0}""{1})", expressionFormat, expressionFormatArguments);
                return expression;
            }

            // Expression: "csharp:"
            if (expression.TrimStart(' ').StartsWith("csharp:"))
            {
                expression = expression.TrimStart(' ').Substring("csharp:".Length);
            }

            return expression;
        }

        protected void WriteToGlobals(string format, params object[] args)
        {
            if (args != null)
                format = string.Format(format, args);
            m_GlobalsBody.AppendFormat(
                @"{0}{1}{2}",
                Environment.NewLine, m_GlobalsBodyTabs, format);
        }

        protected void WriteToBody(string format, params object[] args)
        {
            if (args != null)
                format = string.Format(format, args);
            m_RendererBody.AppendFormat(
                @"{0}{1}{2}",
                Environment.NewLine, m_RendererBodyTabs, format);
        }

        protected string ResolveTypeName(Type type)
        {
            string typeName = "";
            if (m_GlobalsTypeNames.ContainsKey(type.FullName))
            {
                typeName = m_GlobalsTypeNames[type.FullName];
            }
            else
            {
                if (type.IsGenericType)
                {
                    typeName = string.Format("{0}.{1}<", type.Namespace, type.Name.Split('`')[0]);
                    Type[] typeArguments = type.GetGenericArguments();
                    bool first = true;
                    foreach (Type typeArg in typeArguments)
                    {
                        if (!typeArg.IsGenericParameter)
                        {
                            if (!first)
                            {
                                typeName = string.Format("{0}, ", typeName);
                            }
                            first = false;
                            string typeArgTypeName = ResolveTypeName(typeArg);
                            typeName = string.Format("{0}{1}", typeName, typeArgTypeName);
                        }
                        else
                        {
                            // TODO: ???
                        }
                    }
                    typeName = string.Format("{0}>", typeName);
                }
                else
                {
                    typeName = type.FullName.Replace("+", ".");
                }
                m_GlobalsTypeNames[type.FullName] = typeName;
            }
            return typeName;
        }
    }
}
