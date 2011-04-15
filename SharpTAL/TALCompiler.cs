//
// TALCompiler.cs
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
using System.Text.RegularExpressions;
using System.Xml;

namespace SharpTAL
{
    public sealed class Constants
    {
        // This represents a Default value
        public const string DEFAULTVALUE = "{2716EE39-1C18-4f7c-BDD2-5CF89A4EFFE9}";

        internal const string DEFAULT_VALUE_EXPRESSION = "default";

        // Name-space URIs
        internal const string METAL_NAME_URI = "http://xml.zope.org/namespaces/metal";
        internal const string TAL_NAME_URI = "http://xml.zope.org/namespaces/tal";

        //-----------------------------------------------------------
        // All commands are of the form (opcode, args, commandList)
        // The numbers are the opcodes, and also the order of priority.
        // Commands are sorted by the opcode number.
        //
        // TAL does not use use the order in which statements are written in the
        // tag to determine the order in which they are executed.  When an
        // element has multiple statements, they are executed in this order:
        //
        //	tal:define
        //	tal:condition
        //	tal:repeat
        //	tal:content or tal:replace
        //	tal:omit-tag
        //	tal:attributes
        //
        // There is a reasoning behind this ordering.  Because users often want
        // to set up variables for use in other statements contained within this
        // element or subelements, ``tal:define`` is executed first.
        // ``tal:condition`` follows, then ``tal:repeat`` , then ``tal:content``
        // or ``tal:replace``. Finally, before ``tal:attributes``, we have
        // ``tal:omit-tag`` (which is implied with ``tal:replace``).
        //-----------------------------------------------------------

        // Argument: [(DefineAction (global, local, set), variableName, variablePath),...]
        internal const int TAL_DEFINE = 1;
        // Argument: expression, endTagSymbol
        internal const int TAL_CONDITION = 2;
        // Argument: (varname, expression, endTagSymbol)
        internal const int TAL_REPEAT = 3;
        // Argument: (replaceFlag, type, expression)
        internal const int TAL_CONTENT = 4;
        // Not used in byte code, only ordering.
        internal const int TAL_REPLACE = 5;
        // Argument: [(attributeName, expression)]
        internal const int TAL_ATTRIBUTES = 6;
        // Argument: expression
        internal const int TAL_OMITTAG = 7;
        // Argument: (originalAttributeList, currentAttributeList, omitTagScopeFlag)
        internal const int TAL_START_SCOPE = 8;
        // Argument: String to output
        internal const int TAL_OUTPUT = 9;
        // Argument: None
        internal const int TAL_STARTTAG = 10;
        // Argument: Tag, omitTagFlag, omitTagScopeFlag
        internal const int TAL_ENDTAG_ENDSCOPE = 11;
        // Argument: None
        internal const int TAL_NOOP = 13;

        // METAL Starts here
        // Argument: expression, slotParams, macroParams, endTagSymbol
        internal const int METAL_USE_MACRO = 14;
        // Argument: macroName, endTagSymbol
        internal const int METAL_DEFINE_SLOT = 15;
        // Only used for parsing
        internal const int METAL_FILL_SLOT = 16;
        internal const int METAL_DEFINE_MACRO = 17;
        // Argument: [(paramType, paramName, paramPath),...]
        internal const int METAL_DEFINE_PARAM = 18;
        // Argument: [(paramName, paramPath),...]
        internal const int METAL_FILL_PARAM = 19;
        // Argument: [([importNs] importPath),...], endTagSymbol
        internal const int METAL_IMPORT = 20;

        internal static readonly Regex TAL_DEFINE_REGEX = new Regex("(?<!;);(?!;)");
        internal static readonly Regex TAL_ATTRIBUTES_REGEX = new Regex("(?<!;);(?!;)");
        internal static readonly Regex METAL_NAME_REGEX = new Regex("[a-zA-Z_][a-zA-Z0-9_]*");
        internal static readonly Regex METAL_DEFINE_PARAM_REGEX = new Regex("(?<!;);(?!;)");
        internal static readonly Regex METAL_FILL_PARAM_REGEX = new Regex("(?<!;);(?!;)");
        internal static readonly Regex METAL_IMPORT_REGEX = new Regex("(?<!;);(?!;)");

        // The list of elements in HTML that can not have end tags - done as a dictionary for fast
        // lookup.	
        internal static readonly Dictionary<string, int> HTML_FORBIDDEN_ENDTAG = new Dictionary<string, int> {
			{"AREA", 1},
			{"BASE", 1},
			{"BASEFONT", 1},
			{"BR", 1},
			{"COL", 1},
			{"FRAME", 1},
			{"HR", 1},
			{"IMG", 1},
			{"INPUT", 1},
			{"ISINDEX", 1},
			{"LINK", 1},
			{"META", 1},
			{"PARAM", 1}
			};

        internal static string GetCommandName(int commandID)
        {
            if (commandID == Constants.TAL_DEFINE)
                return "TAL_DEFINE";
            if (commandID == Constants.TAL_CONDITION)
                return "TAL_CONDITION";
            if (commandID == Constants.TAL_REPEAT)
                return "TAL_REPEAT";
            if (commandID == Constants.TAL_CONTENT)
                return "TAL_CONTENT";
            if (commandID == Constants.TAL_REPLACE)
                return "TAL_REPLACE";
            if (commandID == Constants.TAL_ATTRIBUTES)
                return "TAL_ATTRIBUTES";
            if (commandID == Constants.TAL_OMITTAG)
                return "TAL_OMITTAG";
            if (commandID == Constants.TAL_START_SCOPE)
                return "TAL_START_SCOPE";
            if (commandID == Constants.TAL_OUTPUT)
                return "TAL_OUTPUT";
            if (commandID == Constants.TAL_STARTTAG)
                return "TAL_STARTTAG";
            if (commandID == Constants.TAL_ENDTAG_ENDSCOPE)
                return "TAL_ENDTAG_ENDSCOPE";
            if (commandID == Constants.TAL_NOOP)
                return "TAL_NOOP";
            if (commandID == Constants.METAL_USE_MACRO)
                return "METAL_USE_MACRO";
            if (commandID == Constants.METAL_DEFINE_SLOT)
                return "METAL_DEFINE_SLOT";
            if (commandID == Constants.METAL_FILL_SLOT)
                return "METAL_FILL_SLOT";
            if (commandID == Constants.METAL_DEFINE_MACRO)
                return "METAL_DEFINE_MACRO";
            if (commandID == Constants.METAL_DEFINE_PARAM)
                return "METAL_DEFINE_PARAM";
            if (commandID == Constants.METAL_FILL_PARAM)
                return "METAL_FILL_PARAM";
            if (commandID == Constants.METAL_IMPORT)
                return "METAL_IMPORT";
            return "UNDEFINED_COMMAND";
        }
    }

    public enum DefineAction
    {
        Local = 1,
        SetLocal = 2,
        Global = 3
    }

    public class DefineInfo
    {
        public DefineAction defAction;
        public string varType;
        public string varName;
        public string varPath;
    }

    public class TagInfo
    {
        public Tag Tag;
        public Dictionary<string, object> Properties;
        public int UseMacroLocation;
    }

    public class Tag
    {
        public string Name;
        public Dictionary<string, string> Attributes;
        public int LineNumber = -1;
        public int LinePosition = -1;
        public string SourcePath;
        public bool OmitTagScope = false;

        public override string ToString()
        {
            string result = "<";
            result += this.Name;
            foreach (KeyValuePair<string, string> att in this.Attributes)
            {
                result += " ";
                result += att.Key;
                result += "='";
                result += att.Value;
                result += "'";
            }
            result += ">";
            return result;
        }
    }

    public class TALCommand
    {
        Tag tag = null;
        public int ID;
        public List<object> Attributes;

        public Tag Tag
        {
            set
            {
                if (value != null)
                {
                    this.tag = new Tag();
                    this.tag.Name = value.Name;
                    this.tag.Attributes = new Dictionary<string, string>(value.Attributes);
                    this.tag.SourcePath = value.SourcePath;
                    this.tag.LineNumber = value.LineNumber;
                    this.tag.LinePosition = value.LinePosition;
                    this.tag.OmitTagScope = value.OmitTagScope;
                }
            }
            get
            {
                return tag;
            }
        }
    }

    public delegate TALCommand CompileCommandDelegate(string argument);
    public delegate void VoidFuncDelegate();

    public class TALCompiler
    {
        public static void CompileTemplate(TemplateInfo ti)
        {
            ti.Programs = new Dictionary<string, TALProgram>();
            ti.ImportedPrograms = new Dictionary<string, TALProgram>();
            ti.ImportedNamespaces = new Dictionary<string, HashSet<string>>();

            TALCompiler compiler = new TALCompiler();

            // Compile main template
            TALProgram mainProg = compiler.Compile(ti.TemplateBody, "<main template>");
            ti.Programs.Add("template", mainProg);

            // Compile imports of main template
            CompileImports(compiler, mainProg, ti);

            // Compile inline templates
            if (ti.InlineTemplates != null)
            {
                foreach (string key in ti.InlineTemplates.Keys)
                {
                    TALProgram inlineProg = compiler.Compile(ti.InlineTemplates[key], string.Format("<inline template: {0}>", key));
                    ti.Programs.Add(key, inlineProg);

                    // Compile Imports of inline template
                    CompileImports(compiler, inlineProg, ti);
                }
            }

            // Compute template hash
            ti.TemplateHash = ComputeTemplateHash(ti);
        }

        static void CompileImports(TALCompiler compiler, TALProgram program, TemplateInfo ti)
        {
            if (program.Imports != null && program.Imports.Count > 0)
            {
                foreach (string key in program.Imports)
                {
                    // Split the Import key
                    string destNs = key.Split(new char[] { ':' }, 2)[0];
                    string sourcePath = key.Split(new char[] { ':' }, 2)[1];

                    // Imported macros without namespace go into main template namespace.
                    if (string.IsNullOrEmpty(destNs))
                    {
                        destNs = "template";
                    }

                    // Check if the template on path was not compiled
                    TALProgram importedProg = null;
                    if (!ti.ImportedPrograms.ContainsKey(sourcePath))
                    {
                        // Compile Imported template
                        string source = File.ReadAllText(sourcePath);
                        importedProg = compiler.Compile(source, sourcePath);
                        ti.ImportedPrograms.Add(sourcePath, importedProg);

                        // Compile Imports of Imported template
                        CompileImports(compiler, importedProg, ti);
                    }
                    else
                    {
                        importedProg = ti.ImportedPrograms[sourcePath];
                    }

                    // Save info about Imported program by namespace and path
                    if (!ti.ImportedNamespaces.ContainsKey(destNs))
                    {
                        ti.ImportedNamespaces.Add(destNs, new HashSet<string>() { sourcePath });
                    }
                    else
                    {
                        if (!ti.ImportedNamespaces[destNs].Contains(sourcePath))
                        {
                            ti.ImportedNamespaces[destNs].Add(sourcePath);
                        }
                    }
                }
            }
        }

        static string ComputeTemplateHash(TemplateInfo ti)
        {
            // Template Hash is computed from following parts (and is recalculated if any of the parts is changed):
            //	"Template Body"
            //	"Full Names of Global Types"
            //	"Inline Templates Bodies"
            //	"Imported Templates Bodies"
            //	"Full Names of Referenced Assemblies"

            // Global types
            string globalTypes = "";
            if (ti.GlobalsTypes != null && ti.GlobalsTypes.Count > 0)
            {
                foreach (string varName in ti.GlobalsTypes.Keys)
                {
                    Type type = ti.GlobalsTypes[varName];
                    globalTypes += type.FullName;
                }
            }

            // Inline templates hashes
            string inlineTemplatesHashes = "";
            if (ti.InlineTemplates != null && ti.InlineTemplates.Count > 0)
            {
                foreach (string templateName in ti.InlineTemplates.Keys)
                {
                    inlineTemplatesHashes += Utils.ComputeHash(ti.InlineTemplates[templateName]);
                }
            }

            // Imported templates hashes
            string importedTemplatesHashes = "";
            if (ti.ImportedPrograms != null && ti.ImportedPrograms.Count > 0)
            {
                foreach (string path in ti.ImportedPrograms.Keys)
                {
                    importedTemplatesHashes += Utils.ComputeHash(ti.ImportedPrograms[path].Source);
                }
            }

            // Referenced Assemblies
            string referencedAssemblies = "";
            if (ti.ReferencedAssemblies != null && ti.ReferencedAssemblies.Count > 0)
            {
                foreach (Assembly asm in ti.ReferencedAssemblies)
                {
                    referencedAssemblies += asm.FullName;
                }
            }

            string templateHash = Utils.ComputeHash(Utils.ComputeHash(ti.TemplateBody)
                + globalTypes + inlineTemplatesHashes + importedTemplatesHashes + referencedAssemblies);

            return templateHash;
        }

        protected List<TALCommand> commandList;
        protected List<TagInfo> tagStack;
        protected Dictionary<int, int> symbolLocationTable;
        protected Dictionary<string, TALSubProgram> macroMap;
        protected HashSet<string> imports;
        protected int endTagSymbol;
        protected Dictionary<int, CompileCommandDelegate> commandHandler;
        protected Tag currentStartTag;

        protected string tal_namespace_prefix;
        protected string tal_namespace_omittag;
        protected string tal_namespace_omitscope;
        protected List<string> tal_namespace_prefix_stack;
        protected Dictionary<string, int> tal_attribute_map;

        protected string metal_namespace_prefix;
        protected List<string> metal_namespace_prefix_stack;
        protected Dictionary<string, int> metal_attribute_map;

        public TALCompiler()
        {
            // Tal commands
            this.commandHandler = new Dictionary<int, CompileCommandDelegate>();
            this.commandHandler.Add(Constants.TAL_DEFINE, this.Compile_TAL_DEFINE);
            this.commandHandler.Add(Constants.TAL_CONDITION, this.Compile_TAL_CONDITION);
            this.commandHandler.Add(Constants.TAL_REPEAT, this.Compile_TAL_REPEAT);
            this.commandHandler.Add(Constants.TAL_CONTENT, this.Compile_TAL_CONTENT);
            this.commandHandler.Add(Constants.TAL_REPLACE, this.Compile_TAL_REPLACE);
            this.commandHandler.Add(Constants.TAL_ATTRIBUTES, this.Compile_TAL_ATTRIBUTES);
            this.commandHandler.Add(Constants.TAL_OMITTAG, this.Compile_TAL_OMITTAG);

            // Metal commands
            this.commandHandler.Add(Constants.METAL_USE_MACRO, this.Compile_METAL_USE_MACRO);
            this.commandHandler.Add(Constants.METAL_DEFINE_SLOT, this.Compile_METAL_DEFINE_SLOT);
            this.commandHandler.Add(Constants.METAL_FILL_SLOT, this.Compile_METAL_FILL_SLOT);
            this.commandHandler.Add(Constants.METAL_DEFINE_MACRO, this.Compile_METAL_DEFINE_MACRO);
            this.commandHandler.Add(Constants.METAL_DEFINE_PARAM, this.Compile_METAL_DEFINE_PARAM);
            this.commandHandler.Add(Constants.METAL_FILL_PARAM, this.Compile_METAL_FILL_PARAM);
            this.commandHandler.Add(Constants.METAL_IMPORT, this.Compile_METAL_IMPORT);

            // Default namespaces
            this.setTALPrefix("tal");
            this.tal_namespace_prefix_stack = new List<string>();
            this.tal_namespace_prefix_stack.Add("tal");

            this.setMETALPrefix("metal");
            this.metal_namespace_prefix_stack = new List<string>();
            this.metal_namespace_prefix_stack.Add("metal");
        }

        public TALProgram Compile(string source, string sourcePath)
        {
            // Initialise a template compiler.
            this.commandList = new List<TALCommand>();
            this.tagStack = new List<TagInfo>();
            this.symbolLocationTable = new Dictionary<int, int>();
            this.macroMap = new Dictionary<string, TALSubProgram>();
            this.imports = new HashSet<string>();
            this.endTagSymbol = 1;
            this.currentStartTag = null;

            XmlReader reader = null;
            StringReader inputReader = null;
            try
            {
                inputReader = new StringReader(source);
                XmlTextReader xmlTextReader = new XmlTextReader(inputReader);
                xmlTextReader.XmlResolver = null;
                xmlTextReader.Namespaces = false;

                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.ProhibitDtd = false;

                reader = XmlReader.Create(xmlTextReader, readerSettings);
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        Tag tag = new Tag();
                        tag.SourcePath = sourcePath;
                        IXmlLineInfo li = reader as IXmlLineInfo;
                        if (li != null && li.HasLineInfo())
                        {
                            tag.LineNumber = li.LineNumber;
                            tag.LinePosition = li.LinePosition;
                        }
                        tag.Name = reader.Name;
                        tag.Attributes = new Dictionary<string, string>();
                        string atttribs = "";
                        for (int i = 0; i < reader.AttributeCount; i++)
                        {
                            reader.MoveToAttribute(i);
                            atttribs += string.Format(" {0}=\"{1}\"", reader.Name, reader.Value);
                            tag.Attributes.Add(reader.Name, reader.Value);
                        }
                        reader.MoveToElement();
                        if (reader.IsEmptyElement)
                        {
                            handle_startendtag(tag);
                        }
                        else
                        {
                            handle_starttag(tag);
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement)
                    {
                        Tag tag = new Tag();
                        tag.SourcePath = sourcePath;
                        IXmlLineInfo li = reader as IXmlLineInfo;
                        if (li != null && li.HasLineInfo())
                        {
                            tag.LineNumber = li.LineNumber;
                            tag.LinePosition = li.LinePosition;
                        }
                        tag.Name = reader.Name;
                        handle_endtag(tag);
                    }
                    else if (reader.NodeType == XmlNodeType.DocumentType)
                    {
                        handle_doctype(reader);
                    }
                    else if (reader.NodeType == XmlNodeType.Comment)
                    {
                        handle_comment(reader.Value);
                    }
                    else if (reader.NodeType == XmlNodeType.CDATA)
                    {
                        handle_cdata(reader.Value);
                    }
                    else if (reader.NodeType == XmlNodeType.XmlDeclaration)
                    {
                        handle_xmldecl(reader.Name, reader.Value);
                    }
                    else if (reader.NodeType == XmlNodeType.Whitespace ||
                        reader.NodeType == XmlNodeType.SignificantWhitespace ||
                        reader.NodeType == XmlNodeType.Text)
                    {
                        handle_data(reader.Value);
                    }
                    else if (reader.NodeType == XmlNodeType.ProcessingInstruction)
                    {
                        handle_pi(reader.Name, reader.Value);
                    }
                    else if (reader.NodeType == XmlNodeType.EntityReference)
                    {
                        handle_entityref(reader.Name);
                    }
                }
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                if (inputReader != null)
                    inputReader.Close();
            }

            TALProgram template = new TALProgram(source, sourcePath, this.commandList, this.macroMap, this.imports, this.symbolLocationTable);
            return template;
        }

        protected void LogCommand(TALCommand cmd)
        {
            List<string> attributes = new List<string>();
            if (cmd.Attributes != null)
            {
                foreach (object attr in cmd.Attributes)
                {
                    attributes.Add(string.Format(@"{0}    {1}", Environment.NewLine, attr));
                }
            }
            Console.WriteLine("{0}Command: {1}{2}",
                Environment.NewLine, Constants.GetCommandName(cmd.ID), string.Join("", attributes.ToArray()));
        }

        protected void AddCommand(TALCommand command)
        {
            // <DEBUG>
            //this.LogCommand(command);
            // </DEBUG>

            //
            // Only following commands can be used inside Tags without scope:
            //	TAL_DEFINE, TAL_OUTPUT
            // Following commands are ignored by the "SourceGenerator" class:
            //	TAL_START_SCOPE, TAL_STARTTAG, TAL_ENDTAG_ENDSCOPE, TAL_OMITTAG
            //
            if (command.Tag != null &&
                command.Tag.OmitTagScope == true &&
                command.ID != Constants.TAL_DEFINE &&
                command.ID != Constants.TAL_OUTPUT &&
                command.ID != Constants.TAL_START_SCOPE &&
                command.ID != Constants.TAL_STARTTAG &&
                command.ID != Constants.TAL_ENDTAG_ENDSCOPE &&
                command.ID != Constants.TAL_OMITTAG)
            {
                // This command can not be used inside tag without his own scope
                string msg = string.Format(@"Command ""{0}"" can not be used inside tag <tal:omit-scope>'.",
                    Constants.GetCommandName(command.ID));
                throw new TemplateParseException(this.currentStartTag, msg);
            }

            if (command.ID == Constants.TAL_OUTPUT &&
                this.commandList.Count > 0 &&
                this.commandList[this.commandList.Count - 1].ID == Constants.TAL_OUTPUT)
            {
                // We can combine output commands
                TALCommand cmd = this.commandList[this.commandList.Count - 1];
                foreach (object att in command.Attributes)
                    cmd.Attributes.Add(att);
            }
            else
            {
                this.commandList.Add(command);
            }
        }

        protected TALCommand Compile_TAL_DEFINE(string argument)
        {
            // Compile a define command, resulting argument is:
            // [(DefineAction (global, local, set), variableName, variablePath),...]
            // Break up the list of defines first
            List<DefineInfo> commandArgs = new List<DefineInfo>();
            // We only want to match semi-colons that are not escaped
            foreach (string defStmt in Constants.TAL_DEFINE_REGEX.Split(argument))
            {
                //  remove any leading space and un-escape any semi-colons
                string defineStmt = defStmt.TrimStart().Replace(";;", ";");
                // Break each defineStmt into pieces "[local|global] varName expression"
                List<string> stmtBits = new List<string>(defineStmt.Split(new char[] { ' ' }));
                DefineAction defAction = DefineAction.Local;
                string varName;
                string expression;
                if (stmtBits.Count < 2)
                {
                    // Error, badly formed define command
                    string msg = string.Format("Badly formed define command '{0}'.  Define commands must be of the form: '[local|global] varName expression[;[local|global] varName expression]'", argument);
                    throw new TemplateParseException(this.currentStartTag, msg);
                }
                // Assume to start with that >2 elements means a local|global flag
                if (stmtBits.Count > 2)
                {
                    if (stmtBits[0] == "global")
                    {
                        defAction = DefineAction.Global;
                        varName = stmtBits[1];
                        expression = string.Join(" ", stmtBits.GetRange(2, stmtBits.Count - 2).ToArray());
                    }
                    else if (stmtBits[0] == "local")
                    {
                        varName = stmtBits[1];
                        expression = string.Join(" ", stmtBits.GetRange(2, stmtBits.Count - 2).ToArray());
                    }
                    else if (stmtBits[0] == "set")
                    {
                        defAction = DefineAction.SetLocal;
                        varName = stmtBits[1];
                        expression = string.Join(" ", stmtBits.GetRange(2, stmtBits.Count - 2).ToArray());
                    }
                    else
                    {
                        // Must be a space in the expression that caused the >3 thing
                        varName = stmtBits[0];
                        expression = string.Join(" ", stmtBits.GetRange(1, stmtBits.Count - 1).ToArray());
                    }
                }
                else
                {
                    // Only two bits
                    varName = stmtBits[0];
                    expression = string.Join(" ", stmtBits.GetRange(1, stmtBits.Count - 1).ToArray());
                }

                DefineInfo di = new DefineInfo();
                di.defAction = defAction;
                di.varName = varName;
                di.varPath = expression;
                commandArgs.Add(di);
            }

            TALCommand ci = new TALCommand();
            ci.Tag = this.currentStartTag;
            ci.ID = Constants.TAL_DEFINE;
            ci.Attributes = new List<object>();
            ci.Attributes.Add(commandArgs);
            return ci;
        }

        protected TALCommand Compile_TAL_CONDITION(string argument)
        {
            // Compile a condition command, resulting argument is:
            // path, endTagSymbol
            // Sanity check
            if (argument.Length == 0)
            {
                // No argument passed
                string msg = "No argument passed!  condition commands must be of the form: 'path'";
                throw new TemplateParseException(this.currentStartTag, msg);
            }

            TALCommand ci = new TALCommand();
            ci.Tag = this.currentStartTag;
            ci.ID = Constants.TAL_CONDITION;
            ci.Attributes = new List<object>();
            ci.Attributes.Add(argument);
            ci.Attributes.Add(this.endTagSymbol);
            return ci;
        }

        protected TALCommand Compile_TAL_REPEAT(string argument)
        {
            // Compile a repeat command, resulting argument is:
            // (varname, expression, endTagSymbol)
            List<string> attProps = new List<string>(argument.Split(new char[] { ' ' }));
            if (attProps.Count < 2)
            {
                // Error, badly formed repeat command
                string msg = string.Format("Badly formed repeat command '{0}'.  Repeat commands must be of the form: 'localVariable path'", argument);
                throw new TemplateParseException(this.currentStartTag, msg);
            }

            string varName = attProps[0];
            string expression = string.Join(" ", attProps.GetRange(1, attProps.Count - 1).ToArray());

            TALCommand ci = new TALCommand();
            ci.Tag = this.currentStartTag;
            ci.ID = Constants.TAL_REPEAT;
            ci.Attributes = new List<object>();
            ci.Attributes.Add(varName);
            ci.Attributes.Add(expression);
            ci.Attributes.Add(this.endTagSymbol);
            return ci;
        }

        protected TALCommand Compile_TAL_CONTENT(string argument)
        {
            return Compile_TAL_CONTENT(argument, 0);
        }

        protected TALCommand Compile_TAL_CONTENT(string argument, int replaceFlag)
        {
            // Compile a content command, resulting argument is
            // (replaceFlag, structureFlag, expression, endTagSymbol)

            // Sanity check
            if (argument.Length == 0)
            {
                // No argument passed
                string msg = "No argument passed!  content/replace commands must be of the form: 'path'";
                throw new TemplateParseException(this.currentStartTag, msg);
            }

            int structureFlag = 0;
            string[] attProps = argument.Split(new char[] { ' ' });
            string express = "";
            if (attProps.Length > 1)
            {
                if (attProps[0] == "structure")
                {
                    structureFlag = 1;
                    express = string.Join(" ", attProps, 1, attProps.Length - 1);
                }
                else if (attProps[1] == "text")
                {
                    structureFlag = 0;
                    express = string.Join(" ", attProps, 1, attProps.Length - 1);
                }
                else
                {
                    // It's not a type selection after all - assume it's part of the path
                    express = argument;
                }
            }
            else
                express = argument;

            TALCommand ci = new TALCommand();
            ci.Tag = this.currentStartTag;
            ci.ID = Constants.TAL_CONTENT;
            ci.Attributes = new List<object>();
            ci.Attributes.Add(replaceFlag);
            ci.Attributes.Add(structureFlag);
            ci.Attributes.Add(express);
            ci.Attributes.Add(this.endTagSymbol);
            return ci;
        }

        protected TALCommand Compile_TAL_REPLACE(string argument)
        {
            return this.Compile_TAL_CONTENT(argument, 1);
        }

        protected TALCommand Compile_TAL_ATTRIBUTES(string argument)
        {
            // Compile tal:attributes into attribute command
            // Argument: [(attributeName, expression)]

            // Break up the list of attribute settings first
            Dictionary<string, string> commandArgs = new Dictionary<string, string>();
            // We only want to match semi-colons that are not escaped
            foreach (string attStmt in Constants.TAL_ATTRIBUTES_REGEX.Split(argument))
            {
                //  remove any leading space and un-escape any semi-colons
                string attributeStmt = attStmt.TrimStart().Replace(";;", ";");
                // Break each attributeStmt into name and expression
                List<string> stmtBits = new List<string>(attributeStmt.Split(new char[] { ' ' }));
                if (stmtBits.Count < 2)
                {
                    // Error, badly formed attributes command
                    string msg = string.Format("Badly formed attributes command '{0}'.  Attributes commands must be of the form: 'name expression[;name expression]'", argument);
                    throw new TemplateParseException(this.currentStartTag, msg);
                }
                string attName = stmtBits[0];
                string attExpr = string.Join(" ", stmtBits.GetRange(1, stmtBits.Count - 1).ToArray());
                commandArgs.Add(attName, attExpr);
            }
            TALCommand ci = new TALCommand();
            ci.Tag = this.currentStartTag;
            ci.ID = Constants.TAL_ATTRIBUTES;
            ci.Attributes = new List<object>();
            ci.Attributes.Add(commandArgs);
            return ci;
        }

        protected TALCommand Compile_TAL_OMITTAG(string argument)
        {
            // Compile a condition command, resulting argument is:
            // path
            // If no argument is given then set the path to default

            string expression = "";
            if (argument.Length == 0)
                expression = Constants.DEFAULT_VALUE_EXPRESSION;
            else
                expression = argument;

            TALCommand ci = new TALCommand();
            ci.Tag = this.currentStartTag;
            ci.ID = Constants.TAL_OMITTAG;
            ci.Attributes = new List<object>();
            ci.Attributes.Add(expression);
            return ci;
        }

        // METAL compilation commands go here
        protected TALCommand Compile_METAL_DEFINE_MACRO(string argument)
        {
            if (argument.Length == 0)
            {
                // No argument passed
                string msg = "No argument passed!  define-macro commands must be of the form: 'define-macro: name'";
                throw new TemplateParseException(this.currentStartTag, msg);
            }

            // Check that the name of the macro is valid
            if (Constants.METAL_NAME_REGEX.Match(argument).Length != argument.Length)
            {
                string msg = string.Format("Macro name {0} is invalid.", argument);
                throw new TemplateParseException(this.currentStartTag, msg);
            }
            if (this.macroMap.ContainsKey(argument))
            {
                string msg = string.Format("Macro name {0} is already defined!", argument);
                throw new TemplateParseException(this.currentStartTag, msg);
            }

            // The macro starts at the next command.
            TALSubProgram macro = new TALSubProgram(this.commandList.Count, this.endTagSymbol);
            this.macroMap.Add(argument, macro);

            return null;
        }

        protected TALCommand Compile_METAL_USE_MACRO(string argument)
        {
            // Sanity check
            if (argument.Length == 0)
            {
                // No argument passed
                string msg = "No argument passed!  use-macro commands must be of the form: 'use-macro: path'";
                throw new TemplateParseException(this.currentStartTag, msg);
            }
            TALCommand ci = new TALCommand();
            ci.Tag = this.currentStartTag;
            ci.ID = Constants.METAL_USE_MACRO;
            ci.Attributes = new List<object>();
            ci.Attributes.Add(argument);
            ci.Attributes.Add(new Dictionary<string, TALSubProgram>());
            ci.Attributes.Add(new List<DefineInfo>());
            ci.Attributes.Add(this.endTagSymbol);
            return ci;
        }

        protected TALCommand Compile_METAL_DEFINE_SLOT(string argument)
        {
            // Compile a define-slot command, resulting argument is:
            // Argument: macroName, endTagSymbol

            if (argument.Length == 0)
            {
                // No argument passed
                string msg = "No argument passed!  define-slot commands must be of the form: 'name'";
                throw new TemplateParseException(this.currentStartTag, msg);
            }
            // Check that the name of the slot is valid
            if (Constants.METAL_NAME_REGEX.Match(argument).Length != argument.Length)
            {
                string msg = string.Format("Slot name {0} is invalid.", argument);
                throw new TemplateParseException(this.currentStartTag, msg);
            }

            TALCommand ci = new TALCommand();
            ci.Tag = this.currentStartTag;
            ci.ID = Constants.METAL_DEFINE_SLOT;
            ci.Attributes = new List<object>();
            ci.Attributes.Add(argument);
            ci.Attributes.Add(this.endTagSymbol);
            return ci;
        }

        protected TALCommand Compile_METAL_FILL_SLOT(string argument)
        {
            if (argument.Length == 0)
            {
                // No argument passed
                string msg = "No argument passed!  fill-slot commands must be of the form: 'fill-slot: name'";
                throw new TemplateParseException(this.currentStartTag, msg);
            }

            // Check that the name of the slot is valid
            if (Constants.METAL_NAME_REGEX.Match(argument).Length != argument.Length)
            {
                string msg = string.Format("Slot name {0} is invalid.", argument);
                throw new TemplateParseException(this.currentStartTag, msg);
            }

            // Determine what use-macro statement this belongs to by working through the list backwards
            int? ourMacroLocation = null;
            int location = this.tagStack.Count - 1;
            while (ourMacroLocation == null)
            {
                int macroLocation = this.tagStack[location].UseMacroLocation;
                if (macroLocation != -1)
                {
                    ourMacroLocation = macroLocation;
                }
                else
                {
                    location -= 1;
                    if (location < 0)
                    {
                        string msg = string.Format("metal:fill-slot must be used inside a metal:use-macro call");
                        throw new TemplateParseException(this.currentStartTag, msg);
                    }
                }
            }

            // Get the use-macro command we are going to adjust
            TALCommand cmnd = this.commandList[(int)ourMacroLocation];
            string macroName = (string)cmnd.Attributes[0];
            Dictionary<string, TALSubProgram> slotMap = (Dictionary<string, TALSubProgram>)cmnd.Attributes[1];
            List<DefineInfo> paramMap = (List<DefineInfo>)cmnd.Attributes[2];
            int endSymbol = (int)cmnd.Attributes[3];

            if (slotMap.ContainsKey(argument))
            {
                string msg = string.Format("Slot {0} has already been filled!", argument);
                throw new TemplateParseException(this.currentStartTag, msg);
            }

            // The slot starts at the next command.
            TALSubProgram slot = new TALSubProgram(this.commandList.Count, this.endTagSymbol);
            slotMap.Add(argument, slot);

            // Update the command
            TALCommand ci = new TALCommand();
            ci.Tag = cmnd.Tag;
            ci.ID = cmnd.ID;
            ci.Attributes = new List<object>();
            ci.Attributes.Add(macroName);
            ci.Attributes.Add(slotMap);
            ci.Attributes.Add(paramMap);
            ci.Attributes.Add(endSymbol);
            this.commandList[(int)ourMacroLocation] = ci;
            return null;
        }

        protected TALCommand Compile_METAL_DEFINE_PARAM(string argument)
        {
            // Compile a define-param command, resulting argument is:
            // Argument: [(paramType, paramName, paramPath),...]

            // Break up the list of defines first
            List<DefineInfo> commandArgs = new List<DefineInfo>();
            // We only want to match semi-colons that are not escaped
            foreach (string defStmt in Constants.METAL_DEFINE_PARAM_REGEX.Split(argument))
            {
                //  remove any leading space and un-escape any semi-colons
                string defineStmt = defStmt.TrimStart().Replace(";;", ";");
                // Break each defineStmt into pieces "[local|global] varName expression"
                List<string> stmtBits = new List<string>(defineStmt.Split(new char[] { ' ' }));
                string varType;
                string varName;
                string expression;
                if (stmtBits.Count < 3)
                {
                    // Error, badly formed define-param command
                    string msg = string.Format("Badly formed define-param command '{0}'.  Define commands must be of the form: 'varType varName expression[;varType varName expression]'", argument);
                    throw new TemplateParseException(this.currentStartTag, msg);
                }
                varType = stmtBits[0];
                varName = stmtBits[1];
                expression = string.Join(" ", stmtBits.GetRange(2, stmtBits.Count - 2).ToArray());

                DefineInfo di = new DefineInfo();
                di.defAction = DefineAction.Local;
                di.varType = varType;
                di.varName = varName;
                di.varPath = expression;
                commandArgs.Add(di);
            }

            TALCommand ci = new TALCommand();
            ci.Tag = this.currentStartTag;
            ci.ID = Constants.METAL_DEFINE_PARAM;
            ci.Attributes = new List<object>();
            ci.Attributes.Add(commandArgs);
            return ci;
        }

        protected TALCommand Compile_METAL_FILL_PARAM(string argument)
        {
            // Compile a fill-param command, resulting argument is:
            // Argument: [(paramName, paramPath),...]

            // Break up the list of defines first
            List<DefineInfo> commandArgs = new List<DefineInfo>();
            // We only want to match semi-colons that are not escaped
            foreach (string defStmt in Constants.METAL_FILL_PARAM_REGEX.Split(argument))
            {
                //  remove any leading space and un-escape any semi-colons
                string defineStmt = defStmt.TrimStart().Replace(";;", ";");
                // Break each defineStmt into pieces "[local|global] varName expression"
                List<string> stmtBits = new List<string>(defineStmt.Split(new char[] { ' ' }));
                string varName;
                string expression;
                if (stmtBits.Count < 2)
                {
                    // Error, badly formed fill-param command
                    string msg = string.Format("Badly formed fill-param command '{0}'.  Fill-param commands must be of the form: 'varName expression[;varName expression]'", argument);
                    throw new TemplateParseException(this.currentStartTag, msg);
                }
                varName = stmtBits[0];
                expression = string.Join(" ", stmtBits.GetRange(1, stmtBits.Count - 1).ToArray());

                DefineInfo di = new DefineInfo();
                di.defAction = DefineAction.Local;
                di.varName = varName;
                di.varPath = expression;
                commandArgs.Add(di);
            }

            // Determine what use-macro statement this belongs to by working through the list backwards
            int? ourMacroLocation = null;
            int location = this.tagStack.Count - 1;
            while (ourMacroLocation == null)
            {
                int macroLocation = this.tagStack[location].UseMacroLocation;
                if (macroLocation != -1)
                {
                    ourMacroLocation = macroLocation;
                }
                else
                {
                    location -= 1;
                    if (location < 0)
                    {
                        string msg = string.Format("metal:fill-param must be used inside a metal:use-macro call");
                        throw new TemplateParseException(this.currentStartTag, msg);
                    }
                }
            }

            // Get the use-macro command we are going to adjust
            TALCommand cmnd = this.commandList[(int)ourMacroLocation];
            string macroName = (string)cmnd.Attributes[0];
            Dictionary<string, TALSubProgram> slotMap = (Dictionary<string, TALSubProgram>)cmnd.Attributes[1];
            List<DefineInfo> paramMap = (List<DefineInfo>)cmnd.Attributes[2];
            int endSymbol = (int)cmnd.Attributes[3];

            // Append param definitions to list
            paramMap.AddRange(commandArgs);

            // Update the command
            TALCommand ci = new TALCommand();
            ci.Tag = cmnd.Tag;
            ci.ID = cmnd.ID;
            ci.Attributes = new List<object>();
            ci.Attributes.Add(macroName);
            ci.Attributes.Add(slotMap);
            ci.Attributes.Add(paramMap);
            ci.Attributes.Add(endSymbol);
            this.commandList[(int)ourMacroLocation] = ci;
            return null;
        }

        protected TALCommand Compile_METAL_IMPORT(string argument)
        {
            // Compile a import command, resulting argument is:
            // Argument: [([importNs] importPath),...], endTagSymbol

            // Sanity check
            if (argument.Length == 0)
            {
                // No argument passed
                string msg = "No argument passed! Metal import commands must be of the form: 'path'";
                throw new TemplateParseException(this.currentStartTag, msg);
            }

            // Break up the list of imports first
            // We only want to match semi-colons that are not escaped
            foreach (string impStmt in Constants.METAL_IMPORT_REGEX.Split(argument))
            {
                //  remove any leading space and un-escape any semi-colons
                string importStmt = impStmt.Trim().Replace(";;", ";");
                string importNs;
                string importPath;
                // Check if import path is legal rooted path
                if (Path.IsPathRooted(importStmt) && File.Exists(importStmt))
                {
                    // We have legal rooted path, no namespace
                    importNs = "";
                    importPath = importStmt;
                }
                else
                {
                    // Break each importStmt into pieces "importNs:importPath"
                    List<string> stmtBits = new List<string>(importStmt.Split(new char[] { ':' }, 2));
                    if (stmtBits.Count < 1)
                    {
                        // Error, badly formed import command
                        string msg = string.Format("Badly formed import command '{0}'.  Import commands must be of the form: '(importNs:)importPath[;(importNs:)importPath]'", argument);
                        throw new TemplateParseException(this.currentStartTag, msg);
                    }
                    // We have namespace
                    if (stmtBits.Count > 1)
                    {
                        importNs = stmtBits[0];
                        importPath = string.Join(" ", stmtBits.GetRange(1, stmtBits.Count - 1).ToArray());
                    }
                    else
                    {
                        // No namespace
                        importNs = "";
                        importPath = stmtBits[0];
                    }
                    // Normalize and check the path to xml stored in path attribute
                    if (!Path.IsPathRooted(importPath))
                    {
                        importPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), importPath));
                    }
                    if (!File.Exists(importPath))
                    {
                        // Invalid path
                        string msg = "Path specified by import argument does not exists: " + importPath;
                        throw new TemplateParseException(this.currentStartTag, msg);
                    }
                }

                // Check if this template was added in this namespace
                string importKey = string.Format("{0}:{1}", importNs, importPath);
                if (!this.imports.Contains(importKey))
                {
                    this.imports.Add(importKey);
                }
            }

            return null;
        }

        protected void handle_startendtag(Tag tag)
        {
            this.handle_starttag(tag, 1);
            if (!Constants.HTML_FORBIDDEN_ENDTAG.ContainsKey(tag.Name.ToUpper()))
                this.handle_endtag(tag);
        }

        protected void handle_starttag(Tag tag)
        {
            handle_starttag(tag, 0);
        }

        protected void handle_starttag(Tag tag, int singletonElement)
        {
            Dictionary<string, string> atts = new Dictionary<string, string>();

            foreach (KeyValuePair<string, string> attInfo in tag.Attributes)
            {
                string att = attInfo.Key;
                string attValue = attInfo.Value;

                // We need to spot empty tal:omit-tags 
                if (attValue == null)
                {
                    if (att == this.tal_namespace_omittag)
                        atts.Add(att, "");
                    else
                        atts.Add(att, att);
                }
                else
                {
                    // Expand any SGML entity references
                    if (attValue.IndexOf('&') != -1 && attValue.IndexOf('&') < attValue.LastIndexOf(';'))
                    {
                        foreach (string entity in SGMLEntityNames.htmlNameToUnicodeNumber.Keys)
                        {
                            if (attValue.Contains(entity))
                            {
                                attValue = attValue.Replace(entity, ((char)SGMLEntityNames.htmlNameToUnicodeNumber[entity]).ToString());
                            }
                        }
                    }
                    atts.Add(att, attValue);
                }
            }

            if (Constants.HTML_FORBIDDEN_ENDTAG.ContainsKey(tag.Name.ToUpper()))
            {
                // This should have no end tag, so we just do the start and suppress the end
                this.parseStartTag(tag, atts, singletonElement);
                this.popTag(tag, 1);
            }
            else
                this.parseStartTag(tag, atts, singletonElement);
        }

        protected void handle_endtag(Tag tag)
        {
            if (Constants.HTML_FORBIDDEN_ENDTAG.ContainsKey(tag.Name.ToUpper()))
            {
                //this.log.warn(string.Format("HTML 4.01 forbids end tags for the {0} element", tag));
            }
            else
            {
                // Normal end tag
                this.popTag(tag);
            }
        }

        protected void handle_data(string data)
        {
            this.parseData(Utils.EscapeXml(data));
        }

        //# These two methods are required so that we expand all character and entity references prior to parsing the template.
        //public void handle_charref (self, ref):
        //    this.parseData (unichr (int (ref)))

        protected void handle_entityref(string data)
        {
            data = string.Format("&{0};", data);
            int code = SGMLEntityNames.htmlNameToUnicodeNumber.ContainsKey(data) ? SGMLEntityNames.htmlNameToUnicodeNumber[data] : 65533;
            char c = Convert.ToChar(code);
            data = string.Format("{0}", c);
            // Use handle_data so that <&> are re-encoded as required.
            this.handle_data(data);
        }

        protected void handle_cdata(string data)
        {
            this.parseData(string.Format("<![CDATA[{0}]]>", data));
        }

        protected void handle_comment(string data)
        {
            this.parseData(string.Format("<!--{0}-->", data));
        }

        protected void handle_doctype(XmlReader reader)
        {
            string public_id = null;
            string system_id = null;
            for (int i = 0; i < reader.AttributeCount; i++)
            {
                reader.MoveToAttribute(i);
                if (reader.Name.ToUpper() == "PUBLIC")
                    public_id = reader.Value;
                else if (reader.Name.ToUpper() == "SYSTEM")
                    system_id = reader.Value;
            }
            reader.MoveToElement();

            string data = "";
            if (public_id != null)
                data = string.Format("<!DOCTYPE {0} PUBLIC \"{1}\" \"{2}\">", reader.Name, public_id, system_id);
            else
                data = string.Format("<!DOCTYPE {0} SYSTEM \"{1}\">", reader.Name, system_id);

            this.parseData(data);
        }

        protected void handle_pi(string name, string data)
        {
            this.parseData(string.Format("<?{0} {1}?>", name, data));
        }

        protected void handle_xmldecl(string name, string data)
        {
            this.parseData(string.Format("<?{0} {1}?>", name, data));
        }

        protected void setTALPrefix(string prefix)
        {
            this.tal_namespace_prefix = prefix;
            this.tal_namespace_omittag = string.Format("{0}:omit-tag", this.tal_namespace_prefix);
            this.tal_namespace_omitscope = string.Format("{0}:omit-scope", this.tal_namespace_prefix);
            this.tal_attribute_map = new Dictionary<string, int>(); ;
            this.tal_attribute_map.Add(string.Format("{0}:attributes", prefix), Constants.TAL_ATTRIBUTES);
            this.tal_attribute_map.Add(string.Format("{0}:content", prefix), Constants.TAL_CONTENT);
            this.tal_attribute_map.Add(string.Format("{0}:define", prefix), Constants.TAL_DEFINE);
            this.tal_attribute_map.Add(string.Format("{0}:replace", prefix), Constants.TAL_REPLACE);
            this.tal_attribute_map.Add(string.Format("{0}:omit-tag", prefix), Constants.TAL_OMITTAG);
            this.tal_attribute_map.Add(string.Format("{0}:condition", prefix), Constants.TAL_CONDITION);
            this.tal_attribute_map.Add(string.Format("{0}:repeat", prefix), Constants.TAL_REPEAT);
        }

        protected void setMETALPrefix(string prefix)
        {
            this.metal_namespace_prefix = prefix;
            this.metal_attribute_map = new Dictionary<string, int>(); ;
            this.metal_attribute_map.Add(string.Format("{0}:define-macro", prefix), Constants.METAL_DEFINE_MACRO);
            this.metal_attribute_map.Add(string.Format("{0}:use-macro", prefix), Constants.METAL_USE_MACRO);
            this.metal_attribute_map.Add(string.Format("{0}:define-slot", prefix), Constants.METAL_DEFINE_SLOT);
            this.metal_attribute_map.Add(string.Format("{0}:fill-slot", prefix), Constants.METAL_FILL_SLOT);
            this.metal_attribute_map.Add(string.Format("{0}:define-param", prefix), Constants.METAL_DEFINE_PARAM);
            this.metal_attribute_map.Add(string.Format("{0}:fill-param", prefix), Constants.METAL_FILL_PARAM);
            this.metal_attribute_map.Add(string.Format("{0}:import", prefix), Constants.METAL_IMPORT);
        }

        protected void popTALNamespace()
        {
            string newPrefix = this.tal_namespace_prefix_stack[this.tal_namespace_prefix_stack.Count - 1];
            this.tal_namespace_prefix_stack.RemoveAt(this.tal_namespace_prefix_stack.Count - 1);
            this.setTALPrefix(newPrefix);
        }

        protected void popMETALNamespace()
        {
            string newPrefix = this.metal_namespace_prefix_stack[this.metal_namespace_prefix_stack.Count - 1];
            this.metal_namespace_prefix_stack.RemoveAt(this.metal_namespace_prefix_stack.Count - 1);
            this.setMETALPrefix(newPrefix);
        }

        public string tagAsText(Tag tag, int singletonFlag)
        {
            // This returns a tag as text.
            //
            string result = "<";
            result += tag.Name;
            foreach (KeyValuePair<string, string> att in tag.Attributes)
            {
                result += " ";
                result += att.Key;
                result += "=\"";
                result += Utils.EscapeXml(att.Value, true);
                result += "\"";
            }
            if (singletonFlag == 1)
                result += " />";
            else
                result += ">";
            return result;
        }

        protected void addTag(Tag tag, Dictionary<string, string> cleanAttributes, Dictionary<string, object> tagProperties)
        {
            // Used to add a tag to the stack.  Various properties can be passed in the dictionary
            // as being information required by the tag.
            // Currently supported properties are:
            //  'command'         - The (command,args) tuple associated with this command
            //  'originalAtts'    - The original attributes that include any metal/tal attributes
            //  'endTagSymbol'    - The symbol associated with the end tag for this element
            //  'popFunctionList' - A list of functions to execute when this tag is popped
            //  'singletonTag'    - A boolean to indicate that this is a singleton flag

            tag.Attributes = cleanAttributes;

            // Add the tag to the tagStack (list of tuples (tag, properties, useMacroLocation))

            TALCommand command = (TALCommand)(tagProperties.ContainsKey("command") ? tagProperties["command"] : null);
            Dictionary<string, string> originalAtts = (Dictionary<string, string>)(tagProperties.ContainsKey("originalAtts") ? tagProperties["originalAtts"] : null);
            int singletonTag = (int)(tagProperties.ContainsKey("singletonTag") ? tagProperties["singletonTag"] : 0);

            TagInfo ti = new TagInfo();
            if (command != null)
            {
                if (command.ID == Constants.METAL_USE_MACRO)
                {
                    ti.Tag = tag;
                    ti.Properties = tagProperties;
                    ti.UseMacroLocation = this.commandList.Count + 1;
                }
                else
                {
                    ti.Tag = tag;
                    ti.Properties = tagProperties;
                    ti.UseMacroLocation = -1;
                }
            }
            else
            {
                ti.Tag = tag;
                ti.Properties = tagProperties;
                ti.UseMacroLocation = -1;
            }
            this.tagStack.Add(ti);

            if (command != null)
            {
                // All tags that have a TAL attribute on them start with a 'start scope'
                TALCommand cmd = new TALCommand();
                cmd.Tag = this.currentStartTag;
                cmd.ID = Constants.TAL_START_SCOPE;
                cmd.Attributes = new List<object>();
                cmd.Attributes.Add(originalAtts);
                cmd.Attributes.Add(cleanAttributes);
                this.AddCommand(cmd);
                // Now we add the TAL command
                this.AddCommand(command);
            }
            else
            {
                // It's just a straight output, so create an output command and append it
                TALCommand cmd = new TALCommand();
                cmd.Tag = this.currentStartTag;
                cmd.ID = Constants.TAL_OUTPUT;
                cmd.Attributes = new List<object>();
                cmd.Attributes.Add(this.tagAsText(tag, singletonTag));
                this.AddCommand(cmd);
            }
        }

        protected void popTag(Tag tag)
        {
            popTag(tag, 0);
        }

        protected void popTag(Tag tag, int omitTagFlag)
        {
            // omitTagFlag is used to control whether the end tag should be included in the
            // output or not.  In HTML 4.01 there are several tags which should never have
            // end tags, this flag allows the template compiler to specify that these
            // should not be output.

            while (this.tagStack.Count > 0)
            {
                TagInfo ti = this.tagStack[this.tagStack.Count - 1];
                this.tagStack.RemoveAt(this.tagStack.Count - 1);

                Tag oldTag = ti.Tag;
                Dictionary<string, object> tagProperties = ti.Properties;

                int? endTagSymbol = (int?)(tagProperties.ContainsKey("endTagSymbol") ? tagProperties["endTagSymbol"] : null);
                List<VoidFuncDelegate> popCommandList = (List<VoidFuncDelegate>)(tagProperties.ContainsKey("popCommandList") ? tagProperties["popCommandList"] : null);
                int singletonTag = (int)(tagProperties.ContainsKey("singletonTag") ? tagProperties["singletonTag"] : 0);

                if (popCommandList != null)
                {
                    foreach (VoidFuncDelegate func in popCommandList)
                    {
                        func();
                    }
                }

                if (oldTag.Name == tag.Name)
                {
                    // We've found the right tag, now check to see if we have any TAL commands on it
                    if (endTagSymbol != null)
                    {
                        // We have a command (it's a TAL tag)
                        // Note where the end tag symbol should point (i.e. the next command)
                        this.symbolLocationTable[(int)endTagSymbol] = this.commandList.Count;

                        // We need a "close scope and tag" command
                        TALCommand cmd = new TALCommand();
                        cmd.Tag = this.currentStartTag;
                        cmd.ID = Constants.TAL_ENDTAG_ENDSCOPE;
                        cmd.Attributes = new List<object>();
                        cmd.Attributes.Add(tag.Name);
                        cmd.Attributes.Add(omitTagFlag);
                        cmd.Attributes.Add(singletonTag);
                        this.AddCommand(cmd);
                        return;
                    }
                    else if (omitTagFlag == 0 && singletonTag == 0)
                    {
                        // We are popping off an un-interesting tag, just add the close as text
                        // We need a "close scope and tag" command
                        TALCommand cmd = new TALCommand();
                        cmd.Tag = this.currentStartTag;
                        cmd.ID = Constants.TAL_OUTPUT;
                        cmd.Attributes = new List<object>();
                        cmd.Attributes.Add("</" + tag.Name + ">");
                        this.AddCommand(cmd);
                        return;
                    }
                    else
                    {
                        // We are suppressing the output of this tag, so just return
                        return;
                    }
                }
                else
                {
                    // We have a different tag, which means something like <br> which never closes is in 
                    // between us and the real tag.

                    // If the tag that we did pop off has a command though it means un-balanced TAL tags!
                    if (endTagSymbol != null)
                    {
                        // ERROR
                        string msg = string.Format("TAL/METAL Elements must be balanced - found close tag {0} expecting {1}", tag.Name, oldTag.Name);
                        throw new TemplateParseException(oldTag, msg);
                    }
                }
            }
            throw new TemplateParseException(null,
                string.Format("</{0}> {1}", tag.Name, "Close tag encountered with no corresponding open tag."));
        }

        protected void parseStartTag(Tag tag, Dictionary<string, string> attributes)
        {
            parseStartTag(tag, attributes, 0);
        }

        protected void parseStartTag(Tag tag, Dictionary<string, string> attributes, int singletonElement)
        {
            // Note down the tag we are handling, it will be used for error handling during
            // compilation
            this.currentStartTag = new Tag();
            this.currentStartTag.Name = tag.Name;
            this.currentStartTag.Attributes = attributes;
            this.currentStartTag.SourcePath = tag.SourcePath;
            this.currentStartTag.LineNumber = tag.LineNumber;
            this.currentStartTag.LinePosition = tag.LinePosition;

            // Look for tal/metal attributes
            List<int> foundTALAtts = new List<int>();
            List<int> foundMETALAtts = new List<int>();
            Dictionary<int, string> foundCommandsArgs = new Dictionary<int, string>();
            Dictionary<string, string> cleanAttributes = new Dictionary<string, string>();
            Dictionary<string, string> originalAttributes = new Dictionary<string, string>();
            Dictionary<string, object> tagProperties = new Dictionary<string, object>();
            List<VoidFuncDelegate> popTagFuncList = new List<VoidFuncDelegate>();
            bool isTALElementNameSpace = false;
            string prefixToAdd = "";
            tagProperties.Add("singletonTag", singletonElement);

            // Determine whether this element is in either the METAL or TAL namespace
            if (tag.Name.IndexOf(':') > 0)
            {
                // We have a namespace involved, so let's look to see if its one of ours
                string _namespace = tag.Name.Substring(0, tag.Name.IndexOf(':'));
                if (_namespace == this.metal_namespace_prefix)
                {
                    isTALElementNameSpace = true;
                    prefixToAdd = this.metal_namespace_prefix + ":";
                }
                else if (_namespace == this.tal_namespace_prefix)
                {
                    // This tag has not his own scope
                    if (tag.Name == tal_namespace_omitscope)
                    {
                        tag.OmitTagScope = true;
                        this.currentStartTag.OmitTagScope = true;
                    }
                    isTALElementNameSpace = true;
                    prefixToAdd = this.tal_namespace_prefix + ":";
                }
                if (isTALElementNameSpace)
                {
                    // We should treat this an implicit omit-tag
                    foundTALAtts.Add(Constants.TAL_OMITTAG);
                    // Will go to default, i.e. yes
                    foundCommandsArgs[Constants.TAL_OMITTAG] = "";
                }
            }

            foreach (KeyValuePair<string, string> attr in attributes)
            {
                string att = attr.Key;
                string value = attr.Value;

                string commandAttName = "";

                originalAttributes.Add(att, value);
                if (isTALElementNameSpace && !(att.IndexOf(':') > 0))
                {
                    // This means that the attribute name does not have a namespace, so use the prefix for this tag.
                    commandAttName = prefixToAdd + att;
                }
                else
                {
                    commandAttName = att;
                }

                if (att.Length > 4 && att.Substring(0, 5) == "xmlns")
                {
                    // We have a namespace declaration.
                    string prefix = att.Length > 5 ? att.Substring(6) : "";
                    if (value == Constants.METAL_NAME_URI)
                    {
                        // It's a METAL namespace declaration
                        if (prefix.Length > 0)
                        {
                            this.metal_namespace_prefix_stack.Add(this.metal_namespace_prefix);
                            this.setMETALPrefix(prefix);
                            // We want this function called when the scope ends
                            popTagFuncList.Add(this.popMETALNamespace);
                        }
                        else
                        {
                            // We don't allow METAL/TAL to be declared as a default
                            string msg = "Can not use METAL name space by default, a prefix must be provided.";
                            throw new TemplateParseException(this.currentStartTag, msg);
                        }
                    }
                    else if (value == Constants.TAL_NAME_URI)
                    {
                        // TAL this time
                        if (prefix.Length > 0)
                        {
                            this.tal_namespace_prefix_stack.Add(this.tal_namespace_prefix);
                            this.setTALPrefix(prefix);
                            // We want this function called when the scope ends
                            popTagFuncList.Add(this.popTALNamespace);
                        }
                        else
                        {
                            // We don't allow METAL/TAL to be declared as a default
                            string msg = "Can not use TAL name space by default, a prefix must be provided.";
                            throw new TemplateParseException(this.currentStartTag, msg);
                        }
                    }
                    else
                    {
                        // It's nothing special, just an ordinary namespace declaration
                        cleanAttributes.Add(att, value);
                    }
                }
                else if (this.tal_attribute_map.ContainsKey(commandAttName))
                {
                    // It's a TAL attribute
                    int cmnd = this.tal_attribute_map[commandAttName];
                    if (cmnd == Constants.TAL_OMITTAG && isTALElementNameSpace)
                    {
                        //this.log.warn("Supressing omit-tag command present on TAL or METAL element");
                    }
                    else
                    {
                        foundCommandsArgs.Add(cmnd, value);
                        foundTALAtts.Add(cmnd);
                    }
                }
                else if (this.metal_attribute_map.ContainsKey(commandAttName))
                {
                    // It's a METAL attribute
                    int cmnd = this.metal_attribute_map[commandAttName];
                    foundCommandsArgs.Add(cmnd, value);
                    foundMETALAtts.Add(cmnd);
                }
                else
                {
                    cleanAttributes.Add(att, value);
                }
            }
            tagProperties.Add("popFunctionList", popTagFuncList);

            // This might be just content
            if ((foundTALAtts.Count + foundMETALAtts.Count) == 0)
            {
                // Just content, add it to the various stacks
                this.addTag(tag, cleanAttributes, tagProperties);
                return;
            }

            // Create a symbol for the end of the tag - we don't know what the offset is yet
            this.endTagSymbol += 1;
            tagProperties.Add("endTagSymbol", this.endTagSymbol);

            // Sort the METAL commands by priority. Priority is defined by opcode number, see Constants.METAL_* opcodes.
            foundMETALAtts.Sort();

            // Sort the TAL commands by priority. Priority is defined by opcode number, see Constants.TAL_* opcodes.
            foundTALAtts.Sort();

            // We handle the METAL before the TAL
            List<int> allCommands = new List<int>();
            allCommands.AddRange(foundMETALAtts);
            allCommands.AddRange(foundTALAtts);
            int firstTag = 1;
            foreach (int talAtt in allCommands)
            {
                // Parse and create a command for each 
                TALCommand cmnd = this.commandHandler[talAtt](foundCommandsArgs[talAtt]);
                if (cmnd != null)
                {
                    if (firstTag == 1)
                    {
                        // The first one needs to add the tag
                        firstTag = 0;
                        tagProperties["originalAtts"] = originalAttributes;
                        tagProperties["command"] = cmnd;
                        this.addTag(tag, cleanAttributes, tagProperties);
                    }
                    else
                    {
                        // All others just append
                        this.AddCommand(cmnd);
                    }
                }
            }

            TALCommand cmd = new TALCommand();
            cmd.Tag = this.currentStartTag;
            cmd.ID = Constants.TAL_STARTTAG;
            cmd.Attributes = new List<object>();
            cmd.Attributes.Add(tag);
            cmd.Attributes.Add(singletonElement);

            if (firstTag == 1)
            {
                tagProperties["originalAtts"] = originalAttributes;
                tagProperties["command"] = cmd;
                this.addTag(tag, cleanAttributes, tagProperties);
            }
            else
            {
                // Add the start tag command in as a child of the last TAL command
                this.AddCommand(cmd);
            }
        }

        protected void parseEndTag(Tag tag)
        {
            // Just pop the tag and related commands off the stack.
            this.popTag(tag);
        }

        protected void parseData(string data)
        {
            // Just add it as an output
            TALCommand cmd = new TALCommand();
            cmd.Tag = this.currentStartTag;
            cmd.ID = Constants.TAL_OUTPUT;
            cmd.Attributes = new List<object>();
            cmd.Attributes.Add(data);
            this.AddCommand(cmd);
        }
    }

    public class TALProgram
    {
        protected string m_source;
        protected string m_sourcePath;
        protected List<TALCommand> m_commandList;
        protected Dictionary<int, int> m_symbolTable;
        protected Dictionary<string, TALSubProgram> m_macros;
        protected HashSet<string> m_imports;

        public string Source
        {
            get { return m_source; }
        }

        public string SourcePath
        {
            get { return m_sourcePath; }
        }

        public List<TALCommand> CommandList
        {
            get { return m_commandList; }
        }

        public Dictionary<int, int> SymbolTable
        {
            get { return m_symbolTable; }
        }

        public Dictionary<string, TALSubProgram> Macros
        {
            get { return this.m_macros; }
        }

        public HashSet<string> Imports
        {
            get { return this.m_imports; }
        }

        public TALProgram(string source, string sourcePath, List<TALCommand> commands, Dictionary<string, TALSubProgram> macros, HashSet<string> imports, Dictionary<int, int> symbols)
        {
            this.m_source = source;
            this.m_sourcePath = sourcePath;
            this.m_commandList = commands;
            this.m_macros = macros;
            this.m_imports = imports;
            this.m_symbolTable = symbols;

            // Setup the macros
            if (this.Macros != null)
            {
                foreach (TALSubProgram macro in this.Macros.Values)
                {
                    macro.SetParentTemplate(this);
                }
            }

            // Setup the slots
            if (this.m_commandList != null)
            {
                foreach (TALCommand cmnd in this.m_commandList)
                {
                    if (cmnd.ID == Constants.METAL_USE_MACRO)
                    {
                        // Set the parent of each slot
                        Dictionary<string, TALSubProgram> slotMap = (Dictionary<string, TALSubProgram>)cmnd.Attributes[1];
                        foreach (TALSubProgram slot in slotMap.Values)
                        {
                            slot.SetParentTemplate(this);
                        }
                    }
                }
            }
        }

        internal virtual List<object> GetProgram()
        {
            // Returns a tuple of (commandList, startPoint, endPoint, symbolTable)
            return new List<object>() { this.m_commandList, 0, this.m_commandList.Count, this.m_symbolTable };
        }
    }

    public class TALSubProgram : TALProgram
    {
        protected TALProgram m_parentTemplate;
        protected int m_startRange;
        protected int m_endTagSymbol;

        public int StartRange { get { return m_startRange; } }
        public int EndTagSymbol { get { return m_endTagSymbol; } }

        // A SubTemplate is part of another template, and is used for the METAL implementation.
        //    The two uses for this class are:
        //        1 - metal:define-macro results in a SubTemplate that is the macro
        //        2 - metal:fill-slot results in a SubTemplate that is a parameter to metal:use-macro
        public TALSubProgram(int startRange, int endTagSymbol)
            : base(null, null, null, null, null, null)
        {
            // The parentTemplate is the template for which we are a sub-template.
            //    The startRange and endRange are indexes into the parent templates command list, 
            //    and defines the range of commands that we can execute
            this.m_startRange = startRange;
            this.m_endTagSymbol = endTagSymbol;
        }

        public void SetParentTemplate(TALProgram parentProgram)
        {
            this.m_parentTemplate = parentProgram;
            this.m_source = parentProgram.Source;
            this.m_sourcePath = parentProgram.SourcePath;
            this.m_commandList = parentProgram.CommandList;
            this.m_symbolTable = parentProgram.SymbolTable;
        }

        internal override List<object> GetProgram()
        {
            // Returns a tuple of (commandList, startPoint, endPoint, symbolTable)
            return new List<object>() { this.m_commandList, this.m_startRange, this.m_symbolTable[this.m_endTagSymbol] + 1, this.m_symbolTable };
        }

        public override string ToString()
        {
            int endRange = this.m_symbolTable[this.m_endTagSymbol];
            string result = string.Format("SubTemplate from {0} to {1}", this.m_startRange, endRange);
            return result;
        }
    }
}
