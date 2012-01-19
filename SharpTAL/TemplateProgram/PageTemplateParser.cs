//
// PageTemplateParser.cs
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

namespace SharpTAL.TemplateProgram
{
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.IO;
	using System.Reflection;
	using System.Text.RegularExpressions;
	using SharpTAL.TemplateParser;
	using SharpTAL.TemplateProgram.Commands;

	public enum TALDefineAction
	{
		Local = 1,
		NonLocal = 2,
		Global = 3
	}

	public class TALDefineInfo
	{
		public TALDefineAction Action { get; set; }
		public string Type { get; set; }
		public string Name { get; set; }
		public string Expression { get; set; }
	}

	/// <summary>
	/// ZPT (Zope Page Template) parser
	/// </summary>
	public class PageTemplateParser : AbstractTemplateParser
	{
		class TagStackItem
		{
			public TagStackItem(Tag tag)
			{
				Tag = tag;
				EndTagCommandLocation = null;
				PopFunctionList = null;
				UseMacroCommandLocation = -1;
			}

			public Tag Tag { get; set; }
			public int? EndTagCommandLocation { get; set; }
			public List<Action> PopFunctionList { get; set; }
			public int UseMacroCommandLocation { get; set; }
		}

		/// <summary>
		/// Contains compiled template programs. The key is the template body hash.
		/// </summary>
		static Dictionary<string, Program> templateProgramCache = new Dictionary<string, Program>();
		static object templateProgramCacheLock = new object();

		const string MAIN_PROGRAM_NAMESPACE = "template";
		const string MAIN_TEMPLATE_PATH = "<main>";
		const string DEFAULT_VALUE_EXPRESSION = "default";

		static readonly Regex TAL_DEFINE_REGEX = new Regex("(?<!;);(?!;)");
		static readonly Regex TAL_ATTRIBUTES_REGEX = new Regex("(?<!;);(?!;)");
		static readonly Regex METAL_DEFINE_PARAM_REGEX = new Regex("(?<!;);(?!;)");
		static readonly Regex METAL_FILL_PARAM_REGEX = new Regex("(?<!;);(?!;)");
		static readonly Regex METAL_IMPORT_REGEX = new Regex("(?<!;);(?!;)");
		static readonly Regex METAL_NAME_REGEX = new Regex("[a-zA-Z_][a-zA-Z0-9_]*");

		static readonly Dictionary<string, string> defaultNamespaces = new Dictionary<string, string> {
				{ "xmlns", Namespaces.XMLNS_NS },
				{ "xml", Namespaces.XML_NS },
				{ "meta", Namespaces.META_NS},
				{ "tal", Namespaces.TAL_NS },
				{ "metal", Namespaces.METAL_NS } };

		Dictionary<CommandType, Func<List<TagAttribute>, List<Command>>> talAttributeHandlers;

		// Per-template compiling state (including inline templates compiling)
		string meta_namespace_prefix;
		List<string> meta_namespace_prefix_stack;
		Dictionary<string, CommandType> meta_attribute_map;
		string tal_namespace_prefix;
		List<string> tal_namespace_prefix_stack;
		Dictionary<string, CommandType> tal_attribute_map;
		string metal_namespace_prefix;
		List<string> metal_namespace_prefix_stack;
		Dictionary<string, CommandType> metal_attribute_map;

		// Per-template-body compiling state
		HashSet<string> importMacroCommands = null;
		List<Command> programCommands;
		Dictionary<int, int> endTagsCommandMap;
		Dictionary<string, IProgram> macroMap;
		List<TagStackItem> tagStack;
		int endTagCommandLocationCounter;
		Tag currentStartTag;

		public PageTemplateParser()
		{
			talAttributeHandlers = new Dictionary<CommandType, Func<List<TagAttribute>, List<Command>>>();
			talAttributeHandlers.Add(CommandType.META_INTERPOLATION, Handle_META_INTERPOLATION);
			talAttributeHandlers.Add(CommandType.METAL_USE_MACRO, Handle_METAL_USE_MACRO);
			talAttributeHandlers.Add(CommandType.METAL_DEFINE_SLOT, Handle_METAL_DEFINE_SLOT);
			talAttributeHandlers.Add(CommandType.METAL_FILL_SLOT, Handle_METAL_FILL_SLOT);
			talAttributeHandlers.Add(CommandType.METAL_DEFINE_MACRO, Handle_METAL_DEFINE_MACRO);
			talAttributeHandlers.Add(CommandType.METAL_DEFINE_PARAM, Handle_METAL_DEFINE_PARAM);
			talAttributeHandlers.Add(CommandType.METAL_FILL_PARAM, Handle_METAL_FILL_PARAM);
			talAttributeHandlers.Add(CommandType.METAL_IMPORT, Handle_METAL_IMPORT);
			talAttributeHandlers.Add(CommandType.TAL_DEFINE, Handle_TAL_DEFINE);
			talAttributeHandlers.Add(CommandType.TAL_CONDITION, Handle_TAL_CONDITION);
			talAttributeHandlers.Add(CommandType.TAL_REPEAT, Handle_TAL_REPEAT);
			talAttributeHandlers.Add(CommandType.TAL_CONTENT, Handle_TAL_CONTENT);
			talAttributeHandlers.Add(CommandType.TAL_REPLACE, Handle_TAL_REPLACE);
			talAttributeHandlers.Add(CommandType.TAL_ATTRIBUTES, Handle_TAL_ATTRIBUTES);
			talAttributeHandlers.Add(CommandType.TAL_OMITTAG, Handle_TAL_OMITTAG);
		}

		public void GenerateTemplateProgram(ref TemplateInfo ti)
		{
			// Init per-template compiling state (including inline templates compiling)
			// Default namespaces
			SetMETAPrefix("meta");
			meta_namespace_prefix_stack = new List<string>();
			meta_namespace_prefix_stack.Add("meta");
			SetTALPrefix("tal");
			tal_namespace_prefix_stack = new List<string>();
			tal_namespace_prefix_stack.Add("tal");
			SetMETALPrefix("metal");
			metal_namespace_prefix_stack = new List<string>();
			metal_namespace_prefix_stack.Add("metal");

			ti.ImportedPrograms = new Dictionary<string, Program>();
			ti.ImportedNamespaces = new Dictionary<string, HashSet<string>>();

			// Compile main template body
			ti.MainProgram = GetTemplateProgram(ti.TemplateBody, MAIN_TEMPLATE_PATH);

			// Compile imported templates
			CompileImportedTemplates(ti, ti.MainProgram);
		}

		void CompileImportedTemplates(TemplateInfo ti, Program program)
		{
			foreach (string importCmd in program.ImportMacroCommands)
			{
				// Parse import command
				string programNamespace = importCmd.Split(new char[] { ':' }, 2)[0];

				// TODO: Cesta k sablone, ktoru chcem importnut je relativna voci sablone, v ktorej volam import, takze tu musim vyskaldat absolutnu cestu
				//	ak je sablona vytvorena zo stringu, treba takej sablone nastavit cestu podla assembly kde v ktorej sa vytvorila, ...alebo ?
				string templatePath = importCmd.Split(new char[] { ':' }, 2)[1];

				Program importedProgram;
				ti.ImportedPrograms.TryGetValue(templatePath, out importedProgram);

				// Compile template program from template body
				if (importedProgram == null)
				{
					// TODO: Implement the template loader (see TODO.txt) - load from filesystem by default
					string templateBody = File.ReadAllText(templatePath);
					importedProgram = GetTemplateProgram(templateBody, templatePath);
					ti.ImportedPrograms.Add(templatePath, importedProgram);
				}

				// Compile imports of imported template
				CompileImportedTemplates(ti, importedProgram);

				// Save info about Imported program by namespace and path
				if (!ti.ImportedNamespaces.ContainsKey(programNamespace))
					ti.ImportedNamespaces.Add(programNamespace, new HashSet<string>() { templatePath });
				else if (!ti.ImportedNamespaces[programNamespace].Contains(templatePath))
					ti.ImportedNamespaces[programNamespace].Add(templatePath);
			}
		}

		Program GetTemplateProgram(string templateBody, string templatePath)
		{
			// Init per-template-body compiling state
			importMacroCommands = new HashSet<string>();

			// Try to get template program from cache
			string bodyHash = Utils.ComputeHash(templateBody);
			Program program = null;
			lock (templateProgramCacheLock)
			{
				if (templateProgramCache.TryGetValue(bodyHash, out program))
					return program;
			}
			if (program != null)
				return program;

			// Per-template-body compiling state
			programCommands = new List<Command>();
			endTagsCommandMap = new Dictionary<int, int>();
			macroMap = new Dictionary<string, IProgram>();
			tagStack = new List<TagStackItem>();
			endTagCommandLocationCounter = 0;
			currentStartTag = null;

			// Parse template
			ParseTemplate(templateBody, templatePath, defaultNamespaces);

			// Create template program instance
			program = new Program(templateBody, templatePath, bodyHash, programCommands, endTagsCommandMap, macroMap, importMacroCommands);

			// Put template program to cache
			lock (templateProgramCacheLock)
			{
				if (!templateProgramCache.ContainsKey(bodyHash))
					templateProgramCache.Add(bodyHash, program);
			}

			return program;
		}

		#region AbstractTemplateParser implementation

		protected override void HandleStartTag(Tag tag)
		{
			// Note down the tag we are handling, it will be used for error handling during compilation
			currentStartTag = new Tag(tag);

			// Expand HTML entity references in attribute values
			foreach (TagAttribute att in currentStartTag.Attributes)
				att.Value = att.UnescapedValue;

			// Sorted dictionary of TAL attributes grouped by attribute type. The dictionary is sorted by the attribute type.
			SortedDictionary<CommandType, List<TagAttribute>> talAttributesDictionary = new SortedDictionary<CommandType, List<TagAttribute>>(new CommandTypeComparer());
			// Clean HTML/XML attributes
			List<TagAttribute> cleanAttributes = new List<TagAttribute>();
			List<Action> popFunctionList = new List<Action>();
			bool isTALElementNameSpace = false;
			string prefixToAdd = "";

			// Resolve TAL/METAL namespace declarations from attributes
			foreach (var att in currentStartTag.Attributes)
			{
				if (att.Name.Length > 4 && att.Name.Substring(0, 5) == "xmlns")
				{
					// We have a namespace declaration.
					string prefix = att.Name.Length > 5 ? att.Name.Substring(6) : "";
					if (att.Value == Namespaces.META_NS)
					{
						// It's a META namespace declaration
						if (prefix.Length > 0)
						{
							meta_namespace_prefix_stack.Add(meta_namespace_prefix);
							SetMETAPrefix(prefix);
							// We want this function called when the scope ends
							popFunctionList.Add(PopMETANamespace);
						}
						else
						{
							// We don't allow META/METAL/TAL to be declared as a default
							string msg = "Can not use META name space by default, a prefix must be provided.";
							throw new TemplateParseException(currentStartTag, msg);
						}
					}
					else if (att.Value == Namespaces.METAL_NS)
					{
						// It's a METAL namespace declaration
						if (prefix.Length > 0)
						{
							metal_namespace_prefix_stack.Add(metal_namespace_prefix);
							SetMETALPrefix(prefix);
							// We want this function called when the scope ends
							popFunctionList.Add(PopMETALNamespace);
						}
						else
						{
							// We don't allow META/METAL/TAL to be declared as a default
							string msg = "Can not use METAL name space by default, a prefix must be provided.";
							throw new TemplateParseException(currentStartTag, msg);
						}
					}
					else if (att.Value == Namespaces.TAL_NS)
					{
						// TAL this time
						if (prefix.Length > 0)
						{
							tal_namespace_prefix_stack.Add(tal_namespace_prefix);
							SetTALPrefix(prefix);
							// We want this function called when the scope ends
							popFunctionList.Add(PopTALNamespace);
						}
						else
						{
							// We don't allow META/METAL/TAL to be declared as a default
							string msg = "Can not use TAL name space by default, a prefix must be provided.";
							throw new TemplateParseException(currentStartTag, msg);
						}
					}
					else
					{
						// It's nothing special, just an ordinary namespace declaration
						cleanAttributes.Add(att);
					}
				}
			}

			// Determine whether this element is in either the METAL or TAL namespace
			if (tag.Name.IndexOf(':') > 0)
			{
				// We have a namespace involved, so let's look to see if its one of ours
				string _namespace = tag.Name.Substring(0, tag.Name.IndexOf(':'));
				if (_namespace == metal_namespace_prefix)
				{
					isTALElementNameSpace = true;
					prefixToAdd = metal_namespace_prefix + ":";
				}
				else if (_namespace == tal_namespace_prefix)
				{
					isTALElementNameSpace = true;
					prefixToAdd = tal_namespace_prefix + ":";
				}
				if (isTALElementNameSpace)
				{
					// We should treat this an implicit omit-tag
					// Will go to default, i.e. yes
					talAttributesDictionary[CommandType.TAL_OMITTAG] = new List<TagAttribute>() { new TALTagAttribute { Value = "", CommandType = CommandType.TAL_OMITTAG } };
				}
			}

			// Look for TAL/METAL attributes
			foreach (var att in currentStartTag.Attributes)
			{
				if (att.Name.Length > 4 && att.Name.Substring(0, 5) == "xmlns")
					// We have a namespace declaration.
					continue;

				string talCommandName = "";

				if (isTALElementNameSpace && att.Name.IndexOf(':') < 0)
					// This means that the attribute name does not have a namespace, so use the prefix for this tag.
					talCommandName = prefixToAdd + att.Name;
				else
					talCommandName = att.Name;

				if (tal_attribute_map.ContainsKey(talCommandName))
				{
					// It's a TAL attribute
					CommandType cmdType = tal_attribute_map[talCommandName];
					if (cmdType == CommandType.TAL_OMITTAG && isTALElementNameSpace)
					{
						// Supressing omit-tag command present on TAL or METAL element
					}
					else
					{
						if (!talAttributesDictionary.ContainsKey(cmdType))
							talAttributesDictionary.Add(cmdType, new List<TagAttribute>());
						talAttributesDictionary[cmdType].Add(new TALTagAttribute(att) { CommandType = cmdType });
					}
				}
				else if (metal_attribute_map.ContainsKey(talCommandName))
				{
					// It's a METAL attribute
					CommandType cmdType = metal_attribute_map[talCommandName];
					if (!talAttributesDictionary.ContainsKey(cmdType))
						talAttributesDictionary.Add(cmdType, new List<TagAttribute>());
					talAttributesDictionary[cmdType].Add(new TALTagAttribute(att) { CommandType = cmdType });
				}
				else if (meta_attribute_map.ContainsKey(talCommandName))
				{
					// It's a META attribute
					CommandType cmdType = meta_attribute_map[talCommandName];
					if (!talAttributesDictionary.ContainsKey(cmdType))
						talAttributesDictionary.Add(cmdType, new List<TagAttribute>());
					talAttributesDictionary[cmdType].Add(new TALTagAttribute(att) { CommandType = cmdType });
				}
				else
				{
					// It's normal HTML/XML attribute
					cleanAttributes.Add(att);
				}
			}

			if (cleanAttributes.Count > 0)
			{
				// Insert normal HTML/XML attributes BEFORE other TAL/METAL TAL_ATTRIBUTES commands
				// as fake TAL_ATTRIBUTES commands to enable string expressions interpolation on normal HTML/XML attributes.
				if (!talAttributesDictionary.ContainsKey(CommandType.TAL_ATTRIBUTES))
					talAttributesDictionary.Add(CommandType.TAL_ATTRIBUTES, new List<TagAttribute>());
				talAttributesDictionary[CommandType.TAL_ATTRIBUTES].InsertRange(0, cleanAttributes);
			}

			// Create a symbol for the end of the tag - we don't know what the offset is yet
			endTagCommandLocationCounter++;

			TagStackItem tagStackItem = null;
			foreach (CommandType cmdType in talAttributesDictionary.Keys)
			{
				// Resolve program commands from tal attributes
				foreach (Command cmd in talAttributeHandlers[cmdType](talAttributesDictionary[cmdType]))
				{
					if (tagStackItem == null)
					{
						// The first command needs to add the tag to the tag stack
						tagStackItem = AddTagToStack(tag, cleanAttributes);

						// Save metal:use-macro command position
						if (cmd.CommandType == CommandType.METAL_USE_MACRO)
							tagStackItem.UseMacroCommandLocation = programCommands.Count + 1;

						// Append command to create new scope for the tag
						Command startScopeCmd = new Command(currentStartTag, CommandType.CMD_START_SCOPE);
						AddCommand(startScopeCmd);
					}

					// All others just append
					AddCommand(cmd);
				}
			}

			if (tagStackItem == null)
			{
				tagStackItem = AddTagToStack(tag, cleanAttributes);

				// Append command to create new scope for the tag
				Command startScopeCmd = new Command(currentStartTag, CommandType.CMD_START_SCOPE);
				AddCommand(startScopeCmd);
			}

			// Save pop functions and end tag command location for this tag
			tagStackItem.PopFunctionList = popFunctionList;
			tagStackItem.EndTagCommandLocation = endTagCommandLocationCounter;

			// Finally, append start tag command
			Command startTagCmd = new Command(currentStartTag, CommandType.CMD_START_TAG);
			AddCommand(startTagCmd);
		}

		protected override void HandleEndTag(Tag tag)
		{
			while (tagStack.Count > 0)
			{
				TagStackItem tagStackItem = tagStack.Last();
				tagStack.RemoveAt(tagStack.Count - 1);

				Tag oldTag = tagStackItem.Tag;

				int? endTagCommandLocation = tagStackItem.EndTagCommandLocation;
				List<Action> popFunctionList = tagStackItem.PopFunctionList;

				if (popFunctionList != null)
				{
					foreach (Action func in popFunctionList)
						func();
				}

				if (oldTag.Name == tag.Name)
				{
					// We've found the right tag, now check to see if we have any TAL commands on it
					if (endTagCommandLocation != null)
					{
						// We have a command (it's a TAL tag)
						// Note where the end tag command location should point (i.e. the next command)
						endTagsCommandMap[(int)endTagCommandLocation] = programCommands.Count;

						// We need a "close scope and tag" command
						Command cmd = new Command(tag, CommandType.CMD_ENDTAG_ENDSCOPE);
						AddCommand(cmd);
						return;
					}
					else if (!tag.Singleton)
					{
						// We are popping off an un-interesting tag, just add the close as text
						// We need a "close scope and tag" command
						Command cmd = new Command(tag, CommandType.CMD_OUTPUT, "</" + tag.Name + ">");
						AddCommand(cmd);
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
					if (endTagCommandLocation != null)
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

		protected override void HandleData(string data)
		{
			// Just add it as an output
			Command cmd = new Command(currentStartTag, CommandType.CMD_OUTPUT, data);
			AddCommand(cmd);
		}

		protected override void HandleComment(string data)
		{
			HandleData(data);
		}

		protected override void HandleDefault(string data)
		{
			HandleData(data);
		}

		#endregion

		TagStackItem AddTagToStack(Tag tag, List<TagAttribute> cleanAttributes)
		{
			// Set tag attributes to contain only normal HTML/XML attributes (TAL/METAL attributes are removed)
			tag.Attributes = cleanAttributes;

			// Add tag to tag stack
			TagStackItem tagStackItem = new TagStackItem(tag);
			tagStack.Add(tagStackItem);
			return tagStackItem;
		}

		void AddCommand(Command command)
		{
			if (command.CommandType == CommandType.CMD_OUTPUT &&
				programCommands.Count > 0 &&
				programCommands[programCommands.Count - 1].CommandType == CommandType.CMD_OUTPUT)
			{
				// We can combine output commands
				Command cmd = programCommands[programCommands.Count - 1];
				foreach (object att in command.Parameters)
					cmd.Parameters.Add(att);
			}
			else
			{
				programCommands.Add(command);
			}
		}

		void SetMETAPrefix(string prefix)
		{
			meta_namespace_prefix = prefix;
			meta_attribute_map = new Dictionary<string, CommandType>(); ;
			meta_attribute_map.Add(string.Format("{0}:interpolation", prefix), CommandType.META_INTERPOLATION);
		}

		void SetTALPrefix(string prefix)
		{
			tal_namespace_prefix = prefix;
			tal_attribute_map = new Dictionary<string, CommandType>(); ;
			tal_attribute_map.Add(string.Format("{0}:attributes", prefix), CommandType.TAL_ATTRIBUTES);
			tal_attribute_map.Add(string.Format("{0}:content", prefix), CommandType.TAL_CONTENT);
			tal_attribute_map.Add(string.Format("{0}:define", prefix), CommandType.TAL_DEFINE);
			tal_attribute_map.Add(string.Format("{0}:replace", prefix), CommandType.TAL_REPLACE);
			tal_attribute_map.Add(string.Format("{0}:omit-tag", prefix), CommandType.TAL_OMITTAG);
			tal_attribute_map.Add(string.Format("{0}:condition", prefix), CommandType.TAL_CONDITION);
			tal_attribute_map.Add(string.Format("{0}:repeat", prefix), CommandType.TAL_REPEAT);
		}

		void SetMETALPrefix(string prefix)
		{
			metal_namespace_prefix = prefix;
			metal_attribute_map = new Dictionary<string, CommandType>(); ;
			metal_attribute_map.Add(string.Format("{0}:define-macro", prefix), CommandType.METAL_DEFINE_MACRO);
			metal_attribute_map.Add(string.Format("{0}:use-macro", prefix), CommandType.METAL_USE_MACRO);
			metal_attribute_map.Add(string.Format("{0}:define-slot", prefix), CommandType.METAL_DEFINE_SLOT);
			metal_attribute_map.Add(string.Format("{0}:fill-slot", prefix), CommandType.METAL_FILL_SLOT);
			metal_attribute_map.Add(string.Format("{0}:define-param", prefix), CommandType.METAL_DEFINE_PARAM);
			metal_attribute_map.Add(string.Format("{0}:fill-param", prefix), CommandType.METAL_FILL_PARAM);
			metal_attribute_map.Add(string.Format("{0}:import", prefix), CommandType.METAL_IMPORT);
		}

		void PopMETANamespace()
		{
			string newPrefix = meta_namespace_prefix_stack[meta_namespace_prefix_stack.Count - 1];
			meta_namespace_prefix_stack.RemoveAt(meta_namespace_prefix_stack.Count - 1);
			SetMETAPrefix(newPrefix);
		}

		void PopTALNamespace()
		{
			string newPrefix = tal_namespace_prefix_stack[tal_namespace_prefix_stack.Count - 1];
			tal_namespace_prefix_stack.RemoveAt(tal_namespace_prefix_stack.Count - 1);
			SetTALPrefix(newPrefix);
		}

		void PopMETALNamespace()
		{
			string newPrefix = metal_namespace_prefix_stack[metal_namespace_prefix_stack.Count - 1];
			metal_namespace_prefix_stack.RemoveAt(metal_namespace_prefix_stack.Count - 1);
			SetMETALPrefix(newPrefix);
		}

		List<Command> Handle_META_INTERPOLATION(List<TagAttribute> attributes)
		{
			// Only last declared attribute is valid
			string argument = attributes[attributes.Count - 1].Value;

			if (string.IsNullOrEmpty(argument))
			{
				// No argument passed
				string msg = "No argument passed! meta:interpolation command must be of the form: meta:interpolation='true|false'";
				throw new TemplateParseException(currentStartTag, msg);
			}

			if (argument == "true")
				return new List<Command> { new METAInterpolation(currentStartTag, true) };

			if (argument == "false")
				return new List<Command> { new METAInterpolation(currentStartTag, false) };

			throw new TemplateParseException(currentStartTag,
				string.Format("Invalid command value '{0}'. Command meta:interpolation must be of the form: meta:interpolation='true|false'", argument));
		}

		List<Command> Handle_TAL_DEFINE(List<TagAttribute> attributes)
		{
			// Join attributes for commands that support multiple attributes
			string argument = string.Join(";", attributes.Select(a => a.Value).ToArray());

			// We only want to match semi-colons that are not escaped
			List<Command> commands = new List<Command>();
			foreach (string defStmt in TAL_DEFINE_REGEX.Split(argument))
			{
				//  remove any leading space and un-escape any semi-colons
				string defineStmt = defStmt.TrimStart().Replace(";;", ";");

				// Break each defineStmt into pieces "[local|global] varName expression"
				List<string> stmtBits = new List<string>(defineStmt.Split(new char[] { ' ' }));
				TALDefine.VariableScope varScope = TALDefine.VariableScope.Local;
				string varName;
				string expression;
				if (stmtBits.Count < 2)
				{
					// Error, badly formed define command
					string msg = string.Format("Badly formed define command '{0}'.  Define commands must be of the form: '[local|nonlocal|global] varName expression[;[local|nonlocal|global] varName expression]'", argument);
					throw new TemplateParseException(currentStartTag, msg);
				}
				// Assume to start with that >2 elements means a local|global flag
				if (stmtBits.Count > 2)
				{
					if (stmtBits[0] == "global")
					{
						varScope = TALDefine.VariableScope.Global;
						varName = stmtBits[1];
						expression = string.Join(" ", stmtBits.GetRange(2, stmtBits.Count - 2).ToArray());
					}
					else if (stmtBits[0] == "local")
					{
						varName = stmtBits[1];
						expression = string.Join(" ", stmtBits.GetRange(2, stmtBits.Count - 2).ToArray());
					}
					else if (stmtBits[0] == "nonlocal")
					{
						varScope = TALDefine.VariableScope.NonLocal;
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

				commands.Add(new TALDefine(currentStartTag, varScope, varName, expression));
			}

			return commands;
		}

		List<Command> Handle_TAL_CONDITION(List<TagAttribute> attributes)
		{
			// Only last declared attribute is valid
			string argument = attributes[attributes.Count - 1].Value;

			// Compile a condition command, resulting argument is:
			// path, endTagCommandLocation
			// Sanity check
			if (argument.Length == 0)
			{
				// No argument passed
				string msg = "No argument passed!  condition commands must be of the form: 'path'";
				throw new TemplateParseException(currentStartTag, msg);
			}

			Command ci = new Command(currentStartTag, CommandType.TAL_CONDITION, argument, endTagCommandLocationCounter);
			return new List<Command> { ci };
		}

		List<Command> Handle_TAL_REPEAT(List<TagAttribute> attributes)
		{
			// Only last declared attribute is valid
			string argument = attributes[attributes.Count - 1].Value;

			// Compile a repeat command, resulting argument is:
			// (varname, expression, endTagCommandLocation)
			List<string> attProps = new List<string>(argument.Split(new char[] { ' ' }));
			if (attProps.Count < 2)
			{
				// Error, badly formed repeat command
				string msg = string.Format("Badly formed repeat command '{0}'.  Repeat commands must be of the form: 'localVariable path'", argument);
				throw new TemplateParseException(currentStartTag, msg);
			}

			string varName = attProps[0];
			string expression = string.Join(" ", attProps.GetRange(1, attProps.Count - 1).ToArray());

			Command ci = new Command(currentStartTag, CommandType.TAL_REPEAT, varName, expression, endTagCommandLocationCounter);
			return new List<Command> { ci };
		}

		List<Command> Handle_TAL_CONTENT(List<TagAttribute> attributes)
		{
			return Handle_TAL_CONTENT(attributes, 0);
		}

		List<Command> Handle_TAL_CONTENT(List<TagAttribute> attributes, int replaceFlag)
		{
			// Only last declared attribute is valid
			string argument = attributes[attributes.Count - 1].Value;

			// Compile a content command, resulting argument is
			// (replaceFlag, structureFlag, expression, endTagCommandLocation)

			// Sanity check
			if (argument.Length == 0)
			{
				// No argument passed
				string msg = "No argument passed!  content/replace commands must be of the form: 'path'";
				throw new TemplateParseException(currentStartTag, msg);
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

			Command ci = new Command(currentStartTag, CommandType.TAL_CONTENT, replaceFlag, structureFlag, express, endTagCommandLocationCounter);
			return new List<Command> { ci };
		}

		List<Command> Handle_TAL_REPLACE(List<TagAttribute> attributes)
		{
			return Handle_TAL_CONTENT(attributes, 1);
		}

		List<Command> Handle_TAL_ATTRIBUTES(List<TagAttribute> attributes)
		{
			// Compile tal:attributes into attribute command

			List<TagAttribute> attrList = new List<TagAttribute>();
			foreach (TagAttribute att in attributes)
			{
				if (att is TALTagAttribute)
				{
					// This is TAL command attribute
					// Break up the attribute args to list of TALTagAttributes
					// We only want to match semi-colons that are not escaped
					foreach (string attStmt in TAL_ATTRIBUTES_REGEX.Split(att.Value))
					{
						// Remove any leading space and un-escape any semi-colons
						// Break each attributeStmt into name and expression
						List<string> stmtBits = new List<string>(attStmt.TrimStart().Replace(";;", ";").Split(' '));
						if (stmtBits.Count < 2)
						{
							// Error, badly formed attributes command
							string msg = string.Format(
								"Badly formed attributes command '{0}'. Attributes commands must be of the form: 'name expression[;name expression]'",
								att.Value);
							throw new TemplateParseException(currentStartTag, msg);
						}
						TALTagAttribute talTagAttr = new TALTagAttribute
						{
							CommandType = ((TALTagAttribute)att).CommandType,
							Name = stmtBits[0].Trim(' ', '\r', '\n'),
							Value = string.Join(" ", stmtBits.GetRange(1, stmtBits.Count - 1).ToArray()),
							Eq = @"=",
							Quote = @"""",
							QuoteEntity = Utils.Char2Entity(@"""")
						};
						attrList.Add(talTagAttr);
					}
				}
				else
				{
					// This is clean html/xml tag attribute (no TAL/METAL command)
					attrList.Add(att);
				}
			}
			Command cmd = new Command(currentStartTag, CommandType.TAL_ATTRIBUTES, attrList);
			return new List<Command> { cmd };
		}

		List<Command> Handle_TAL_OMITTAG(List<TagAttribute> attributes)
		{
			// Only last declared attribute is valid
			string argument = attributes[attributes.Count - 1].Value;

			// Compile a condition command, resulting argument is:
			// path
			// If no argument is given then set the path to default

			string expression = "";
			if (argument.Length == 0)
				expression = DEFAULT_VALUE_EXPRESSION;
			else
				expression = argument;

			Command ci = new Command(currentStartTag, CommandType.TAL_OMITTAG, expression);
			return new List<Command> { ci };
		}

		List<Command> Handle_METAL_DEFINE_MACRO(List<TagAttribute> attributes)
		{
			// Only last declared attribute is valid
			string macroName = attributes[attributes.Count - 1].Value;

			if (string.IsNullOrEmpty(macroName))
			{
				// No argument passed
				string msg = "No argument passed!  define-macro commands must be of the form: 'define-macro: name'";
				throw new TemplateParseException(currentStartTag, msg);
			}

			// Check that the name of the macro is valid
			if (METAL_NAME_REGEX.Match(macroName).Length != macroName.Length)
			{
				string msg = string.Format("Macro name {0} is invalid.", macroName);
				throw new TemplateParseException(currentStartTag, msg);
			}
			if (macroMap.ContainsKey(macroName))
			{
				string msg = string.Format("Macro name {0} is already defined!", macroName);
				throw new TemplateParseException(currentStartTag, msg);
			}

			// The macro starts at the next command.
			IProgram macro = new ProgramMacro(macroName, programCommands.Count, endTagCommandLocationCounter);
			macroMap.Add(macroName, macro);

			return new List<Command>();
		}

		List<Command> Handle_METAL_USE_MACRO(List<TagAttribute> attributes)
		{
			// Only last declared attribute is valid
			string argument = attributes[attributes.Count - 1].Value;

			// Sanity check
			if (argument.Length == 0)
			{
				// No argument passed
				string msg = "No argument passed!  use-macro commands must be of the form: 'use-macro: path'";
				throw new TemplateParseException(currentStartTag, msg);
			}
			Command cmd = new Command(currentStartTag, CommandType.METAL_USE_MACRO, argument, new Dictionary<string, ProgramSlot>(), new List<TALDefineInfo>(), endTagCommandLocationCounter);
			return new List<Command> { cmd };
		}

		List<Command> Handle_METAL_DEFINE_SLOT(List<TagAttribute> attributes)
		{
			// Only last declared attribute is valid
			string argument = attributes[attributes.Count - 1].Value;

			// Compile a define-slot command, resulting argument is:
			// Argument: macroName, endTagCommandLocation

			if (argument.Length == 0)
			{
				// No argument passed
				string msg = "No argument passed!  define-slot commands must be of the form: 'name'";
				throw new TemplateParseException(currentStartTag, msg);
			}
			// Check that the name of the slot is valid
			if (METAL_NAME_REGEX.Match(argument).Length != argument.Length)
			{
				string msg = string.Format("Slot name {0} is invalid.", argument);
				throw new TemplateParseException(currentStartTag, msg);
			}

			Command cmd = new Command(currentStartTag, CommandType.METAL_DEFINE_SLOT, argument, endTagCommandLocationCounter);
			return new List<Command> { cmd };
		}

		List<Command> Handle_METAL_FILL_SLOT(List<TagAttribute> attributes)
		{
			// Only last declared attribute is valid
			string argument = attributes[attributes.Count - 1].Value;

			if (argument.Length == 0)
			{
				// No argument passed
				string msg = "No argument passed!  fill-slot commands must be of the form: 'fill-slot: name'";
				throw new TemplateParseException(currentStartTag, msg);
			}

			// Check that the name of the slot is valid
			if (METAL_NAME_REGEX.Match(argument).Length != argument.Length)
			{
				string msg = string.Format("Slot name {0} is invalid.", argument);
				throw new TemplateParseException(currentStartTag, msg);
			}

			// Determine what use-macro statement this belongs to by working through the list backwards
			int ourMacroLocation = -1;
			int location = tagStack.Count - 1;
			while (ourMacroLocation == -1)
			{
				int macroLocation = tagStack[location].UseMacroCommandLocation;
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
						throw new TemplateParseException(currentStartTag, msg);
					}
				}
			}

			// Get the use-macro command we are going to adjust
			Command cmnd = programCommands[ourMacroLocation];
			string macroName = (string)cmnd.Parameters[0];
			Dictionary<string, ProgramSlot> slotMap = (Dictionary<string, ProgramSlot>)cmnd.Parameters[1];
			List<TALDefineInfo> paramMap = (List<TALDefineInfo>)cmnd.Parameters[2];
			int endSymbol = (int)cmnd.Parameters[3];

			if (slotMap.ContainsKey(argument))
			{
				string msg = string.Format("Slot {0} has already been filled!", argument);
				throw new TemplateParseException(currentStartTag, msg);
			}

			// The slot starts at the next command.
			ProgramSlot slot = new ProgramSlot(argument, programCommands.Count, endTagCommandLocationCounter);
			slotMap.Add(argument, slot);

			// Update the command
			Command ci = new Command(cmnd.Tag, cmnd.CommandType, macroName, slotMap, paramMap, endSymbol);
			programCommands[ourMacroLocation] = ci;
			return new List<Command>();
		}

		List<Command> Handle_METAL_DEFINE_PARAM(List<TagAttribute> attributes)
		{
			// Join attributes for commands that support multiple attributes
			string argument = string.Join(";", attributes.Select(a => a.Value).ToArray());

			// Compile a define-param command, resulting argument is:
			// Argument: [(paramType, paramName, paramPath),...]

			// Break up the list of defines first
			List<TALDefineInfo> commandArgs = new List<TALDefineInfo>();
			// We only want to match semi-colons that are not escaped
			foreach (string defStmt in METAL_DEFINE_PARAM_REGEX.Split(argument))
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
					throw new TemplateParseException(currentStartTag, msg);
				}
				varType = stmtBits[0];
				varName = stmtBits[1];
				expression = string.Join(" ", stmtBits.GetRange(2, stmtBits.Count - 2).ToArray());

				TALDefineInfo di = new TALDefineInfo();
				di.Action = TALDefineAction.Local;
				di.Type = varType;
				di.Name = varName;
				di.Expression = expression;
				commandArgs.Add(di);
			}

			Command cmd = new Command(currentStartTag, CommandType.METAL_DEFINE_PARAM, commandArgs);
			return new List<Command> { cmd };
		}

		List<Command> Handle_METAL_FILL_PARAM(List<TagAttribute> attributes)
		{
			// Join attributes for commands that support multiple attributes
			string argument = string.Join(";", attributes.Select(a => a.Value).ToArray());

			// Compile a fill-param command, resulting argument is:
			// Argument: [(paramName, paramPath),...]

			// Break up the list of defines first
			List<TALDefineInfo> commandArgs = new List<TALDefineInfo>();
			// We only want to match semi-colons that are not escaped
			foreach (string defStmt in METAL_FILL_PARAM_REGEX.Split(argument))
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
					throw new TemplateParseException(currentStartTag, msg);
				}
				varName = stmtBits[0];
				expression = string.Join(" ", stmtBits.GetRange(1, stmtBits.Count - 1).ToArray());

				TALDefineInfo di = new TALDefineInfo();
				di.Action = TALDefineAction.Local;
				di.Name = varName;
				di.Expression = expression;
				commandArgs.Add(di);
			}

			// Determine what use-macro statement this belongs to by working through the list backwards
			int ourMacroLocation = -1;
			int stackIndex = tagStack.Count - 1;
			while (ourMacroLocation == -1)
			{
				int macroLocation = tagStack[stackIndex].UseMacroCommandLocation;
				if (macroLocation != -1)
				{
					ourMacroLocation = macroLocation;
				}
				else
				{
					stackIndex -= 1;
					if (stackIndex < 0)
					{
						string msg = string.Format("metal:fill-param must be used inside a metal:use-macro call");
						throw new TemplateParseException(currentStartTag, msg);
					}
				}
			}

			// Get the use-macro command we are going to adjust
			Command cmnd = programCommands[ourMacroLocation];
			string macroName = (string)cmnd.Parameters[0];
			Dictionary<string, ProgramSlot> slotMap = (Dictionary<string, ProgramSlot>)cmnd.Parameters[1];
			List<TALDefineInfo> paramMap = (List<TALDefineInfo>)cmnd.Parameters[2];
			int endSymbol = (int)cmnd.Parameters[3];

			// Append param definitions to list
			paramMap.AddRange(commandArgs);

			// Update the command
			Command ci = new Command(cmnd.Tag, cmnd.CommandType, macroName, slotMap, paramMap, endSymbol);
			programCommands[ourMacroLocation] = ci;
			return new List<Command>();
		}

		List<Command> Handle_METAL_IMPORT(List<TagAttribute> attributes)
		{
			// Join attributes for commands that support multiple attributes
			string argument = string.Join(";", attributes.Select(a => a.Value).ToArray());

			// Compile a import command, resulting argument is:
			// Argument: [([importNs] importPath),...], endTagCommandLocation

			// Sanity check
			if (string.IsNullOrEmpty(argument))
			{
				// No argument passed
				string msg = "No argument passed! Metal import commands must be of the form: 'path'";
				throw new TemplateParseException(currentStartTag, msg);
			}

			// Break up the list of imports first
			// We only want to match semi-colons that are not escaped
			foreach (string impStmt in METAL_IMPORT_REGEX.Split(argument))
			{
				//  remove any leading space and un-escape any semi-colons
				string importStmt = impStmt.Trim().Replace(";;", ";");
				string importNs = MAIN_PROGRAM_NAMESPACE;
				string importPath;
				// Check if import path is legal rooted path
				if (Path.IsPathRooted(importStmt) && File.Exists(importStmt))
				{
					// Import statement contains only legal rooted path, no namespace definition
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
						throw new TemplateParseException(currentStartTag, msg);
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
						throw new TemplateParseException(currentStartTag, msg);
					}
				}

				// Save import command to cache
				string importCmd = string.Format("{0}:{1}", importNs, importPath);
				if (!importMacroCommands.Contains(importCmd))
				{
					importMacroCommands.Add(importCmd);
				}
			}

			return new List<Command>();
		}
	}

	// <TODO>
	// Test program interpretation without assembly generation.
	// This will support only dynamic expression types like path: and python: and maybe dquery: (dynamicquery) (no csharp: because this requires assembly generation)
	//public abstract class AbstractProgramInterpreter
	//{
	//    protected Dictionary<CommandType, Action<Command>> commandHandlers;
	//    public AbstractProgramInterpreter()
	//    {
	//        commandHandlers = new Dictionary<CommandType, Action<Command>>();
	//        commandHandlers.Add(CommandType.META_INTERPOLATION, Handle_META_INTERPOLATION);
	//        commandHandlers.Add(CommandType.METAL_USE_MACRO, Handle_METAL_USE_MACRO);
	//        commandHandlers.Add(CommandType.METAL_DEFINE_SLOT, Handle_METAL_DEFINE_SLOT);
	//        commandHandlers.Add(CommandType.METAL_DEFINE_PARAM, Handle_METAL_DEFINE_PARAM);
	//        commandHandlers.Add(CommandType.TAL_DEFINE, Handle_TAL_DEFINE);
	//        commandHandlers.Add(CommandType.TAL_CONDITION, Handle_TAL_CONDITION);
	//        commandHandlers.Add(CommandType.TAL_REPEAT, Handle_TAL_REPEAT);
	//        commandHandlers.Add(CommandType.TAL_CONTENT, Handle_TAL_CONTENT);
	//        commandHandlers.Add(CommandType.TAL_ATTRIBUTES, Handle_TAL_ATTRIBUTES);
	//        commandHandlers.Add(CommandType.TAL_OMITTAG, Handle_TAL_OMITTAG);
	//        commandHandlers.Add(CommandType.CMD_START_SCOPE, Handle_CMD_START_SCOPE);
	//        commandHandlers.Add(CommandType.CMD_OUTPUT, Handle_CMD_OUTPUT);
	//        commandHandlers.Add(CommandType.CMD_START_TAG, Handle_CMD_START_TAG);
	//        commandHandlers.Add(CommandType.CMD_ENDTAG_ENDSCOPE, Handle_CMD_ENDTAG_ENDSCOPE);
	//        commandHandlers.Add(CommandType.CMD_NOOP, Handle_CMD_NOOP);
	//    }
	//    public void Run(IEnumerable<Command> commands)
	//    {
	//        foreach (Command cmd in commands)
	//            commandHandlers[cmd.CommandType](cmd);
	//    }
	//    protected abstract void Handle_META_INTERPOLATION(Command cmd);
	//    protected abstract void Handle_METAL_USE_MACRO(Command cmd);
	//    protected abstract void Handle_METAL_DEFINE_SLOT(Command cmd);
	//    protected abstract void Handle_METAL_FILL_SLOT(Command cmd);
	//    protected abstract void Handle_METAL_DEFINE_MACRO(Command cmd);
	//    protected abstract void Handle_METAL_DEFINE_PARAM(Command cmd);
	//    protected abstract void Handle_METAL_FILL_PARAM(Command cmd);
	//    protected abstract void Handle_METAL_IMPORT(Command cmd);
	//    protected abstract void Handle_TAL_DEFINE(Command cmd);
	//    protected abstract void Handle_TAL_CONDITION(Command cmd);
	//    protected abstract void Handle_TAL_REPEAT(Command cmd);
	//    protected abstract void Handle_TAL_CONTENT(Command cmd);
	//    protected abstract void Handle_TAL_REPLACE(Command cmd);
	//    protected abstract void Handle_TAL_ATTRIBUTES(Command cmd);
	//    protected abstract void Handle_TAL_OMITTAG(Command cmd);
	//    protected abstract void Handle_CMD_START_SCOPE(Command cmd);
	//    protected abstract void Handle_CMD_OUTPUT(Command cmd);
	//    protected abstract void Handle_CMD_START_TAG(Command cmd);
	//    protected abstract void Handle_CMD_ENDTAG_ENDSCOPE(Command cmd);
	//    protected abstract void Handle_CMD_NOOP(Command cmd);
	//}
	// </TODO>
}
