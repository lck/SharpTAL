//
// ProgramGenerator.cs
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

	public class ProgramGenerator : AbstractTemplateParser
	{
		class TagProperties
		{
			public TagProperties()
			{
				EndTagCommandLocation = null;
				PopFunctionList = null;
				Command = null;
				OriginalAttributes = null;
				UseMacroCommandLocation = -1;
			}

			public int? EndTagCommandLocation { get; set; }
			public List<Action> PopFunctionList { get; set; }
			public Command Command { get; set; }
			public Dictionary<string, TagAttribute> OriginalAttributes { get; set; }
			public int UseMacroCommandLocation { get; set; }
		}

		class TagStackItem
		{
			public Tag Tag { get; set; }
			public TagProperties Properties { get; set; }
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

		Dictionary<CommandType, Func<List<TagAttribute>, Command>> commandHandler;

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
		List<Command> commandList;
		Dictionary<int, int> endTagsCommandMap;
		Dictionary<string, IProgram> macroMap;
		List<TagStackItem> tagStack;
		int endTagCommandLocationCounter;
		Tag currentStartTag;

		public ProgramGenerator()
		{
			commandHandler = new Dictionary<CommandType, Func<List<TagAttribute>, Command>>();
			commandHandler.Add(CommandType.META_INTERPOLATION, Handle_META_INTERPOLATION);
			commandHandler.Add(CommandType.METAL_USE_MACRO, Handle_METAL_USE_MACRO);
			commandHandler.Add(CommandType.METAL_DEFINE_SLOT, Handle_METAL_DEFINE_SLOT);
			commandHandler.Add(CommandType.METAL_FILL_SLOT, Handle_METAL_FILL_SLOT);
			commandHandler.Add(CommandType.METAL_DEFINE_MACRO, Handle_METAL_DEFINE_MACRO);
			commandHandler.Add(CommandType.METAL_DEFINE_PARAM, Handle_METAL_DEFINE_PARAM);
			commandHandler.Add(CommandType.METAL_FILL_PARAM, Handle_METAL_FILL_PARAM);
			commandHandler.Add(CommandType.METAL_IMPORT, Handle_METAL_IMPORT);
			commandHandler.Add(CommandType.TAL_DEFINE, Handle_TAL_DEFINE);
			commandHandler.Add(CommandType.TAL_CONDITION, Handle_TAL_CONDITION);
			commandHandler.Add(CommandType.TAL_REPEAT, Handle_TAL_REPEAT);
			commandHandler.Add(CommandType.TAL_CONTENT, Handle_TAL_CONTENT);
			commandHandler.Add(CommandType.TAL_REPLACE, Handle_TAL_REPLACE);
			commandHandler.Add(CommandType.TAL_ATTRIBUTES, Handle_TAL_ATTRIBUTES);
			commandHandler.Add(CommandType.TAL_OMITTAG, Handle_TAL_OMITTAG);
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

		private void CompileImportedTemplates(TemplateInfo ti, Program program)
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

		private Program GetTemplateProgram(string templateBody, string templatePath)
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
			commandList = new List<Command>();
			endTagsCommandMap = new Dictionary<int, int>();
			macroMap = new Dictionary<string, IProgram>();
			tagStack = new List<TagStackItem>();
			endTagCommandLocationCounter = 0;
			currentStartTag = null;

			// Parse template
			ParseTemplate(templateBody, templatePath);

			// Create template program instance
			program = new Program(templateBody, templatePath, bodyHash, commandList, endTagsCommandMap, macroMap, importMacroCommands);

			// Put template program to cache
			lock (templateProgramCacheLock)
			{
				if (!templateProgramCache.ContainsKey(bodyHash))
					templateProgramCache.Add(bodyHash, program);
			}

			return program;
		}

		protected override void HandleStartTag(Tag tag)
		{
			// Note down the tag we are handling, it will be used for error handling during compilation
			currentStartTag = new Tag(tag);

			// Expand HTML entity references in attribute values
			foreach (TagAttribute att in currentStartTag.Attributes)
				att.Value = att.UnescapedValue;

			// Look for TAL/METAL attributes
			SortedDictionary<CommandType, List<TagAttribute>> commands = new SortedDictionary<CommandType, List<TagAttribute>>(new CommandTypeComparer());
			List<TagAttribute> cleanAttributes = new List<TagAttribute>();
			TagProperties tagProperties = new TagProperties();
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
					commands[CommandType.TAL_OMITTAG] = new List<TagAttribute>() { new TALTagAttribute { Value = "", CommandType = CommandType.TAL_OMITTAG } };
				}
			}

			// Resolve TAL/METAL commands from attributes
			foreach (var att in currentStartTag.Attributes)
			{
				if (att.Name.Length > 4 && att.Name.Substring(0, 5) == "xmlns")
					// We have a namespace declaration.
					continue;

				string commandName = "";

				if (isTALElementNameSpace && att.Name.IndexOf(':') < 0)
					// This means that the attribute name does not have a namespace, so use the prefix for this tag.
					commandName = prefixToAdd + att.Name;
				else
					commandName = att.Name;

				if (tal_attribute_map.ContainsKey(commandName))
				{
					// It's a TAL attribute
					CommandType cmdType = tal_attribute_map[commandName];
					if (cmdType == CommandType.TAL_OMITTAG && isTALElementNameSpace)
					{
						// Supressing omit-tag command present on TAL or METAL element
					}
					else
					{
						if (!commands.ContainsKey(cmdType))
							commands.Add(cmdType, new List<TagAttribute>());
						commands[cmdType].Add(new TALTagAttribute(att) { CommandType = cmdType });
					}
				}
				else if (metal_attribute_map.ContainsKey(commandName))
				{
					// It's a METAL attribute
					CommandType cmdType = metal_attribute_map[commandName];
					if (!commands.ContainsKey(cmdType))
						commands.Add(cmdType, new List<TagAttribute>());
					commands[cmdType].Add(new TALTagAttribute(att) { CommandType = cmdType });
				}
				else if (meta_attribute_map.ContainsKey(commandName))
				{
					// It's a META attribute
					CommandType cmdType = meta_attribute_map[commandName];
					if (!commands.ContainsKey(cmdType))
						commands.Add(cmdType, new List<TagAttribute>());
					commands[cmdType].Add(new TALTagAttribute(att) { CommandType = cmdType });
				}
				else
				{
					// It's normal HTML/XML attribute
					cleanAttributes.Add(att);
				}
			}
			tagProperties.PopFunctionList = popFunctionList;

			if (cleanAttributes.Count > 0)
			{
				// Insert normal HTML/XML attributes BEFORE other TAL/METAL attributes of type TAL_ATTRIBUTES
				// as fake TAL_ATTRIBUTES to enable string expressions processing.
				if (!commands.ContainsKey(CommandType.TAL_ATTRIBUTES))
					commands.Add(CommandType.TAL_ATTRIBUTES, new List<TagAttribute>());
				commands[CommandType.TAL_ATTRIBUTES].InsertRange(0, cleanAttributes);
			}

			// Create a symbol for the end of the tag - we don't know what the offset is yet
			endTagCommandLocationCounter++;
			tagProperties.EndTagCommandLocation = endTagCommandLocationCounter;

			bool firstTag = true;
			foreach (CommandType cmdType in commands.Keys)
			{
				// Create command from attributes
				Command cmnd = commandHandler[cmdType](commands[cmdType]);
				if (cmnd != null)
				{
					if (firstTag)
					{
						// The first one needs to add the tag
						firstTag = false;
						tagProperties.Command = cmnd;
						AddTag(tag, cleanAttributes, tagProperties);
					}
					else
					{
						// All others just append
						AddCommand(cmnd);
					}
				}
			}

			Command cmd = new Command(currentStartTag, CommandType.CMD_START_TAG);

			if (firstTag)
			{
				tagProperties.Command = cmd;
				AddTag(tag, cleanAttributes, tagProperties);
			}
			else
			{
				// Add the start tag command in as a child of the last TAL command
				AddCommand(cmd);
			}
		}

		protected override void HandleEndTag(Tag tag)
		{
			while (tagStack.Count > 0)
			{
				TagStackItem tagStackItem = tagStack.Last();
				tagStack.RemoveAt(tagStack.Count - 1);

				Tag oldTag = tagStackItem.Tag;
				TagProperties tagProperties = tagStackItem.Properties;

				int? endTagCommandLocation = tagProperties.EndTagCommandLocation;
				List<Action> popFunctionList = tagProperties.PopFunctionList;

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
						endTagsCommandMap[(int)endTagCommandLocation] = commandList.Count;

						// We need a "close scope and tag" command
						Command cmd = new Command(tag, CommandType.CMD_ENDTAG_ENDSCOPE);
						AddCommand(cmd);
						return;
					}
					else if (!tag.Singleton)
					{
						// We are popping off an un-interesting tag, just add the close as text
						// We need a "close scope and tag" command
						Command cmd = new Command(tag, CommandType.CMD_OUTPUT);
						cmd.Parameters = new List<object>();
						cmd.Parameters.Add("</" + tag.Name + ">");
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
			Command cmd = new Command(currentStartTag, CommandType.CMD_OUTPUT);
			cmd.Parameters = new List<object>();
			cmd.Parameters.Add(data);
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

		void AddTag(Tag tag, List<TagAttribute> cleanAttributes, TagProperties tagProperties)
		{
			// Used to add a tag to the stack.  Various properties can be passed in the dictionary
			// as being information required by the tag.
			// Currently supported properties are:
			//  'command'                - The (command,args) tuple associated with this command
			//  'endTagCommandLocation'    - The symbol associated with the end tag for this element
			//  'popFunctionList'        - A list of functions to execute when this tag is popped

			tag.Attributes = cleanAttributes;

			// Add the tag to the tagStack (list of tuples (tag, properties, useMacroLocation))

			Command command = tagProperties.Command;

			TagStackItem tagStackItem = new TagStackItem();
			if (command != null)
			{
				if (command.CommandType == CommandType.METAL_USE_MACRO)
				{
					tagStackItem.Tag = tag;
					tagStackItem.Properties = tagProperties;
					tagStackItem.Properties.UseMacroCommandLocation = commandList.Count + 1;
				}
				else
				{
					tagStackItem.Tag = tag;
					tagStackItem.Properties = tagProperties;
					tagStackItem.Properties.UseMacroCommandLocation = -1;
				}
			}
			else
			{
				tagStackItem.Tag = tag;
				tagStackItem.Properties = tagProperties;
				tagStackItem.Properties.UseMacroCommandLocation = -1;
			}
			tagStack.Add(tagStackItem);

			if (command != null)
			{
				// All tags that have a TAL attribute on them start with a 'start scope'
				Command cmdStartScope = new Command(currentStartTag, CommandType.CMD_START_SCOPE);
				AddCommand(cmdStartScope);
				// Now we add the TAL command
				AddCommand(command);
			}
			else
			{
				// It's just a straight output, so create an output command and append it
				Command cmd = new Command(currentStartTag, CommandType.CMD_OUTPUT);
				cmd.Parameters = new List<object>();
				cmd.Parameters.Add(tag.Format());
				AddCommand(cmd);
			}
		}

		protected void AddCommand(Command command)
		{
			if (command.CommandType == CommandType.CMD_OUTPUT &&
				commandList.Count > 0 &&
				commandList[commandList.Count - 1].CommandType == CommandType.CMD_OUTPUT)
			{
				// We can combine output commands
				Command cmd = commandList[commandList.Count - 1];
				foreach (object att in command.Parameters)
					cmd.Parameters.Add(att);
			}
			else
			{
				commandList.Add(command);
			}
		}

		protected Command Handle_META_INTERPOLATION(List<TagAttribute> attributes)
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
				return new Command(currentStartTag, CommandType.META_INTERPOLATION, true);

			if (argument == "false")
				return new Command(currentStartTag, CommandType.META_INTERPOLATION, false);

			throw new TemplateParseException(currentStartTag,
				string.Format("Invalid command value '{0}'. Command meta:interpolation must be of the form: meta:interpolation='true|false'", argument));
		}

		protected Command Handle_TAL_DEFINE(List<TagAttribute> attributes)
		{
			// Join attributes for commands that support multiple attributes
			string argument = string.Join(";", attributes.Select(a => a.Value).ToArray());

			// Compile a define command, resulting argument is:
			// [(DefineAction (global, local, set), variableName, variablePath),...]
			// Break up the list of defines first
			List<TALDefineInfo> commandArgs = new List<TALDefineInfo>();
			// We only want to match semi-colons that are not escaped
			foreach (string defStmt in TAL_DEFINE_REGEX.Split(argument))
			{
				//  remove any leading space and un-escape any semi-colons
				string defineStmt = defStmt.TrimStart().Replace(";;", ";");
				// Break each defineStmt into pieces "[local|global] varName expression"
				List<string> stmtBits = new List<string>(defineStmt.Split(new char[] { ' ' }));
				TALDefineAction defAction = TALDefineAction.Local;
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
						defAction = TALDefineAction.Global;
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
						defAction = TALDefineAction.NonLocal;
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

				TALDefineInfo di = new TALDefineInfo();
				di.Action = defAction;
				di.Name = varName;
				di.Expression = expression;
				commandArgs.Add(di);
			}

			Command ci = new Command(currentStartTag, CommandType.TAL_DEFINE);
			ci.Parameters = new List<object>
			{
				commandArgs
			};
			return ci;
		}

		protected Command Handle_TAL_CONDITION(List<TagAttribute> attributes)
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

			Command ci = new Command(currentStartTag, CommandType.TAL_CONDITION);
			ci.Parameters = new List<object>
			{
				argument,
				endTagCommandLocationCounter
			};
			return ci;
		}

		protected Command Handle_TAL_REPEAT(List<TagAttribute> attributes)
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

			Command ci = new Command(currentStartTag, CommandType.TAL_REPEAT);
			ci.Parameters = new List<object>
			{
				varName,
				expression,
				endTagCommandLocationCounter
			};
			return ci;
		}

		protected Command Handle_TAL_CONTENT(List<TagAttribute> attributes)
		{
			return Handle_TAL_CONTENT(attributes, 0);
		}

		protected Command Handle_TAL_CONTENT(List<TagAttribute> attributes, int replaceFlag)
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

			Command ci = new Command(currentStartTag, CommandType.TAL_CONTENT);
			ci.Parameters = new List<object>
			{
				replaceFlag,
				structureFlag,
				express,
				endTagCommandLocationCounter
			};
			return ci;
		}

		protected Command Handle_TAL_REPLACE(List<TagAttribute> attributes)
		{
			return Handle_TAL_CONTENT(attributes, 1);
		}

		protected Command Handle_TAL_ATTRIBUTES(List<TagAttribute> attributes)
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
			return cmd;
		}

		protected Command Handle_TAL_OMITTAG(List<TagAttribute> attributes)
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

			Command ci = new Command(currentStartTag, CommandType.TAL_OMITTAG);
			ci.Parameters = new List<object>
			{
				expression
			};
			return ci;
		}

		// METAL compilation commands go here
		protected Command Handle_METAL_DEFINE_MACRO(List<TagAttribute> attributes)
		{
			// Only last declared attribute is valid
			string argument = attributes[attributes.Count - 1].Value;

			if (argument.Length == 0)
			{
				// No argument passed
				string msg = "No argument passed!  define-macro commands must be of the form: 'define-macro: name'";
				throw new TemplateParseException(currentStartTag, msg);
			}

			// Check that the name of the macro is valid
			if (METAL_NAME_REGEX.Match(argument).Length != argument.Length)
			{
				string msg = string.Format("Macro name {0} is invalid.", argument);
				throw new TemplateParseException(currentStartTag, msg);
			}
			if (macroMap.ContainsKey(argument))
			{
				string msg = string.Format("Macro name {0} is already defined!", argument);
				throw new TemplateParseException(currentStartTag, msg);
			}

			// The macro starts at the next command.
			IProgram macro = new ProgramMacro(argument, commandList.Count, endTagCommandLocationCounter);
			macroMap.Add(argument, macro);

			return null;
		}

		protected Command Handle_METAL_USE_MACRO(List<TagAttribute> attributes)
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
			Command ci = new Command(currentStartTag, CommandType.METAL_USE_MACRO);
			ci.Parameters = new List<object>
			{
				argument,
				new Dictionary<string, ProgramSlot>(),
				new List<TALDefineInfo>(),
				endTagCommandLocationCounter
			};
			return ci;
		}

		protected Command Handle_METAL_DEFINE_SLOT(List<TagAttribute> attributes)
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

			Command ci = new Command(currentStartTag, CommandType.METAL_DEFINE_SLOT);
			ci.Parameters = new List<object>
			{
				argument,
				endTagCommandLocationCounter
			};
			return ci;
		}

		protected Command Handle_METAL_FILL_SLOT(List<TagAttribute> attributes)
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
			int? ourMacroLocation = null;
			int location = tagStack.Count - 1;
			while (ourMacroLocation == null)
			{
				int macroLocation = tagStack[location].Properties.UseMacroCommandLocation;
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
			Command cmnd = commandList[(int)ourMacroLocation];
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
			ProgramSlot slot = new ProgramSlot(argument, commandList.Count, endTagCommandLocationCounter);
			slotMap.Add(argument, slot);

			// Update the command
			Command ci = new Command(cmnd.Tag, cmnd.CommandType);
			ci.Parameters = new List<object>
			{
				macroName,
				slotMap,
				paramMap,
				endSymbol
			};
			commandList[(int)ourMacroLocation] = ci;
			return null;
		}

		protected Command Handle_METAL_DEFINE_PARAM(List<TagAttribute> attributes)
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

			Command ci = new Command(currentStartTag, CommandType.METAL_DEFINE_PARAM);
			ci.Parameters = new List<object>
			{
				commandArgs
			};
			return ci;
		}

		protected Command Handle_METAL_FILL_PARAM(List<TagAttribute> attributes)
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
			int? ourMacroLocation = null;
			int location = tagStack.Count - 1;
			while (ourMacroLocation == null)
			{
				int macroLocation = tagStack[location].Properties.UseMacroCommandLocation;
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
						throw new TemplateParseException(currentStartTag, msg);
					}
				}
			}

			// Get the use-macro command we are going to adjust
			Command cmnd = commandList[(int)ourMacroLocation];
			string macroName = (string)cmnd.Parameters[0];
			Dictionary<string, ProgramSlot> slotMap = (Dictionary<string, ProgramSlot>)cmnd.Parameters[1];
			List<TALDefineInfo> paramMap = (List<TALDefineInfo>)cmnd.Parameters[2];
			int endSymbol = (int)cmnd.Parameters[3];

			// Append param definitions to list
			paramMap.AddRange(commandArgs);

			// Update the command
			Command ci = new Command(cmnd.Tag, cmnd.CommandType);
			ci.Parameters = new List<object>
			{
				macroName,
				slotMap,
				paramMap,
				endSymbol
			};
			commandList[(int)ourMacroLocation] = ci;
			return null;
		}

		protected Command Handle_METAL_IMPORT(List<TagAttribute> attributes)
		{
			// Join attributes for commands that support multiple attributes
			string argument = string.Join(";", attributes.Select(a => a.Value).ToArray());

			// Compile a import command, resulting argument is:
			// Argument: [([importNs] importPath),...], endTagCommandLocation

			// Sanity check
			if (argument.Length == 0)
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

			return null;
		}

		protected void SetMETAPrefix(string prefix)
		{
			meta_namespace_prefix = prefix;
			meta_attribute_map = new Dictionary<string, CommandType>(); ;
			meta_attribute_map.Add(string.Format("{0}:interpolation", prefix), CommandType.META_INTERPOLATION);
		}

		protected void SetTALPrefix(string prefix)
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

		protected void SetMETALPrefix(string prefix)
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

		protected void PopMETANamespace()
		{
			string newPrefix = meta_namespace_prefix_stack[meta_namespace_prefix_stack.Count - 1];
			meta_namespace_prefix_stack.RemoveAt(meta_namespace_prefix_stack.Count - 1);
			SetMETAPrefix(newPrefix);
		}

		protected void PopTALNamespace()
		{
			string newPrefix = tal_namespace_prefix_stack[tal_namespace_prefix_stack.Count - 1];
			tal_namespace_prefix_stack.RemoveAt(tal_namespace_prefix_stack.Count - 1);
			SetTALPrefix(newPrefix);
		}

		protected void PopMETALNamespace()
		{
			string newPrefix = metal_namespace_prefix_stack[metal_namespace_prefix_stack.Count - 1];
			metal_namespace_prefix_stack.RemoveAt(metal_namespace_prefix_stack.Count - 1);
			SetMETALPrefix(newPrefix);
		}
	}
}
