//
// ProgramGenerator.cs
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
using System.Text.RegularExpressions;

using SharpTAL.TemplateParser;
using SharpTAL.TemplateProgram.Commands;

namespace SharpTAL.TemplateProgram
{
	/// <summary>
	/// ZPT (Zope Page Template) parser and Template program generator
	/// </summary>
	public class ProgramGenerator : AbstractTemplateParser
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

			public Tag Tag { get; private set; }
			public int? EndTagCommandLocation { get; set; }
			public List<Action> PopFunctionList { get; set; }
			public int UseMacroCommandLocation { get; set; }
		}

		/// <summary>
		/// Contains compiled template programs. The key is the template body hash.
		/// </summary>
		private static readonly Dictionary<string, Program> TemplateProgramCache = new Dictionary<string, Program>();
		private static readonly object TemplateProgramCacheLock = new object();

		private const string MainProgramNamespace = "template";
		private const string MainTemplatePath = "<main>";
		private const string DefaultValueExpression = "default";

		private static readonly Regex TalDefineRegex = new Regex("(?<!;);(?!;)");
		private static readonly Regex TalAttributesRegex = new Regex("(?<!;);(?!;)");
		private static readonly Regex MetalDefineParamRegex = new Regex("(?<!;);(?!;)");
		private static readonly Regex MetalFillParamRegex = new Regex("(?<!;);(?!;)");
		private static readonly Regex MetalImportRegex = new Regex("(?<!;);(?!;)");
		private static readonly Regex MetalNameRegex = new Regex("[a-zA-Z_][a-zA-Z0-9_]*");

		private static readonly Dictionary<string, string> DefaultNamespaces = new Dictionary<string, string> {
				{ "xmlns", Namespaces.XmlnsNs },
				{ "xml", Namespaces.XmlNs },
				{ "meta", Namespaces.MetaNs},
				{ "tal", Namespaces.TalNs },
				{ "metal", Namespaces.MetalNs } };

		private readonly Dictionary<CommandType, Func<List<TagAttribute>, List<Command>>> _talAttributeHandlers;

		// Per-template compiling state (including inline templates compiling)
		private string _metaNamespacePrefix;
		private List<string> _metaNamespacePrefixStack;
		private Dictionary<string, CommandType> _metaAttributeMap;
		private string _talNamespacePrefix;
		private List<string> _talNamespacePrefixStack;
		private Dictionary<string, CommandType> _talAttributeMap;
		private string _metalNamespacePrefix;
		private List<string> _metalNamespacePrefixStack;
		private Dictionary<string, CommandType> _metalAttributeMap;

		// Per-template-body compiling state
		private HashSet<string> _importMacroCommands;
		private List<ICommand> _programCommands;
		private Dictionary<int, int> _endTagsCommandMap;
		private Dictionary<string, IProgram> _macroMap;
		private List<TagStackItem> _tagStack;
		private int _endTagCommandLocationCounter;
		private Tag _currentStartTag;

		public ProgramGenerator()
		{
			_talAttributeHandlers = new Dictionary<CommandType, Func<List<TagAttribute>, List<Command>>>
			{
				{CommandType.MetaInterpolation, Handle_META_INTERPOLATION},
				{CommandType.MetalUseMacro, Handle_METAL_USE_MACRO},
				{CommandType.MetalDefineSlot, Handle_METAL_DEFINE_SLOT},
				{CommandType.MetalFillSlot, Handle_METAL_FILL_SLOT},
				{CommandType.MetalDefineMacro, Handle_METAL_DEFINE_MACRO},
				{CommandType.MetalDefineParam, Handle_METAL_DEFINE_PARAM},
				{CommandType.MetalFillParam, Handle_METAL_FILL_PARAM},
				{CommandType.MetalImport, Handle_METAL_IMPORT},
				{CommandType.TalDefine, Handle_TAL_DEFINE},
				{CommandType.TalCondition, Handle_TAL_CONDITION},
				{CommandType.TalRepeat, Handle_TAL_REPEAT},
				{CommandType.TalContent, Handle_TAL_CONTENT},
				{CommandType.TalReplace, Handle_TAL_REPLACE},
				{CommandType.TalAttributes, Handle_TAL_ATTRIBUTES},
				{CommandType.TalOmittag, Handle_TAL_OMITTAG}
			};
		}

		public void GenerateTemplateProgram(ref TemplateInfo ti)
		{
			// Init per-template compiling state (including inline templates compiling)
			// Default namespaces
			SetMetaPrefix("meta");
			_metaNamespacePrefixStack = new List<string> { "meta" };
			SetTalPrefix("tal");
			_talNamespacePrefixStack = new List<string> { "tal" };
			SetMetalPrefix("metal");
			_metalNamespacePrefixStack = new List<string> { "metal" };

			ti.ImportedPrograms = new Dictionary<string, Program>();
			ti.ImportedNamespaces = new Dictionary<string, HashSet<string>>();

			// Compile main template body
			ti.MainProgram = GetTemplateProgram(ti.TemplateBody, MainTemplatePath);

			// Compile imported templates
			CompileImportedTemplates(ti, ti.MainProgram);
		}

		void CompileImportedTemplates(TemplateInfo ti, Program program)
		{
			foreach (string importCmd in program.ImportMacroCommands)
			{
				// Parse import command
				string programNamespace = importCmd.Split(new[] { ':' }, 2)[0];

				string templatePath = importCmd.Split(new[] { ':' }, 2)[1];

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
					ti.ImportedNamespaces.Add(programNamespace, new HashSet<string> { templatePath });
				else if (!ti.ImportedNamespaces[programNamespace].Contains(templatePath))
					ti.ImportedNamespaces[programNamespace].Add(templatePath);
			}
		}

		Program GetTemplateProgram(string templateBody, string templatePath)
		{
			// Init per-template-body compiling state
			_importMacroCommands = new HashSet<string>();

			// Try to get template program from cache
			string bodyHash = Utils.ComputeHash(templateBody);
			Program program;
			lock (TemplateProgramCacheLock)
			{
				if (TemplateProgramCache.TryGetValue(bodyHash, out program))
					return program;
			}

			// Per-template-body compiling state
			_programCommands = new List<ICommand>();
			_endTagsCommandMap = new Dictionary<int, int>();
			_macroMap = new Dictionary<string, IProgram>();
			_tagStack = new List<TagStackItem>();
			_endTagCommandLocationCounter = 0;
			_currentStartTag = null;

			// Parse template
			ParseTemplate(templateBody, templatePath, DefaultNamespaces);

			// Create template program instance
			program = new Program(templateBody, templatePath, bodyHash, _programCommands, _endTagsCommandMap, _macroMap, _importMacroCommands);

			// Put template program to cache
			lock (TemplateProgramCacheLock)
			{
				if (!TemplateProgramCache.ContainsKey(bodyHash))
					TemplateProgramCache.Add(bodyHash, program);
			}

			return program;
		}

		#region AbstractTemplateParser implementation

		protected override void HandleStartTag(Tag tag)
		{
			// Note down the tag we are handling, it will be used for error handling during compilation
			_currentStartTag = new Tag(tag);

			// Expand HTML entity references in attribute values
			foreach (TagAttribute att in _currentStartTag.Attributes)
				att.Value = att.UnescapedValue;

			// Sorted dictionary of TAL attributes grouped by attribute type. The dictionary is sorted by the attribute type.
			SortedDictionary<CommandType, List<TagAttribute>> talAttributesDictionary = new SortedDictionary<CommandType, List<TagAttribute>>(new CommandTypeComparer());
			// Clean HTML/XML attributes
			var cleanAttributes = new List<TagAttribute>();
			var popFunctionList = new List<Action>();
			bool isTalElementNameSpace = false;
			string prefixToAdd = "";

			// Resolve TAL/METAL namespace declarations from attributes
			foreach (var att in _currentStartTag.Attributes)
			{
				if (att.Name.Length > 4 && att.Name.Substring(0, 5) == "xmlns")
				{
					// We have a namespace declaration.
					string prefix = att.Name.Length > 5 ? att.Name.Substring(6) : "";
					if (att.Value == Namespaces.MetaNs)
					{
						// It's a META namespace declaration
						if (prefix.Length > 0)
						{
							_metaNamespacePrefixStack.Add(_metaNamespacePrefix);
							SetMetaPrefix(prefix);
							// We want this function called when the scope ends
							popFunctionList.Add(PopMetaNamespace);
						}
						else
						{
							// We don't allow META/METAL/TAL to be declared as a default
							const string msg = "Can not use META name space by default, a prefix must be provided.";
							throw new TemplateParseException(_currentStartTag, msg);
						}
					}
					else if (att.Value == Namespaces.MetalNs)
					{
						// It's a METAL namespace declaration
						if (prefix.Length > 0)
						{
							_metalNamespacePrefixStack.Add(_metalNamespacePrefix);
							SetMetalPrefix(prefix);
							// We want this function called when the scope ends
							popFunctionList.Add(PopMetalNamespace);
						}
						else
						{
							// We don't allow META/METAL/TAL to be declared as a default
							const string msg = "Can not use METAL name space by default, a prefix must be provided.";
							throw new TemplateParseException(_currentStartTag, msg);
						}
					}
					else if (att.Value == Namespaces.TalNs)
					{
						// TAL this time
						if (prefix.Length > 0)
						{
							_talNamespacePrefixStack.Add(_talNamespacePrefix);
							SetTalPrefix(prefix);
							// We want this function called when the scope ends
							popFunctionList.Add(PopTalNamespace);
						}
						else
						{
							// We don't allow META/METAL/TAL to be declared as a default
							const string msg = "Can not use TAL name space by default, a prefix must be provided.";
							throw new TemplateParseException(_currentStartTag, msg);
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
				if (_namespace == _metalNamespacePrefix)
				{
					isTalElementNameSpace = true;
					prefixToAdd = _metalNamespacePrefix + ":";
				}
				else if (_namespace == _talNamespacePrefix)
				{
					isTalElementNameSpace = true;
					prefixToAdd = _talNamespacePrefix + ":";
				}
				if (isTalElementNameSpace)
				{
					// We should treat this an implicit omit-tag
					// Will go to default, i.e. yes
					talAttributesDictionary[CommandType.TalOmittag] = new List<TagAttribute> { new TalTagAttribute { Value = "", CommandType = CommandType.TalOmittag } };
				}
			}

			// Look for TAL/METAL attributes
			foreach (var att in _currentStartTag.Attributes)
			{
				if (att.Name.Length > 4 && att.Name.Substring(0, 5) == "xmlns")
					// We have a namespace declaration.
					continue;

				string talCommandName;

				if (isTalElementNameSpace && att.Name.IndexOf(':') < 0)
					// This means that the attribute name does not have a namespace, so use the prefix for this tag.
					talCommandName = prefixToAdd + att.Name;
				else
					talCommandName = att.Name;

				if (_talAttributeMap.ContainsKey(talCommandName))
				{
					// It's a TAL attribute
					CommandType cmdType = _talAttributeMap[talCommandName];
					if (cmdType == CommandType.TalOmittag && isTalElementNameSpace)
					{
						// Supressing omit-tag command present on TAL or METAL element
					}
					else
					{
						if (!talAttributesDictionary.ContainsKey(cmdType))
							talAttributesDictionary.Add(cmdType, new List<TagAttribute>());
						talAttributesDictionary[cmdType].Add(new TalTagAttribute(att) { CommandType = cmdType });
					}
				}
				else if (_metalAttributeMap.ContainsKey(talCommandName))
				{
					// It's a METAL attribute
					CommandType cmdType = _metalAttributeMap[talCommandName];
					if (!talAttributesDictionary.ContainsKey(cmdType))
						talAttributesDictionary.Add(cmdType, new List<TagAttribute>());
					talAttributesDictionary[cmdType].Add(new TalTagAttribute(att) { CommandType = cmdType });
				}
				else if (_metaAttributeMap.ContainsKey(talCommandName))
				{
					// It's a META attribute
					CommandType cmdType = _metaAttributeMap[talCommandName];
					if (!talAttributesDictionary.ContainsKey(cmdType))
						talAttributesDictionary.Add(cmdType, new List<TagAttribute>());
					talAttributesDictionary[cmdType].Add(new TalTagAttribute(att) { CommandType = cmdType });
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
				if (!talAttributesDictionary.ContainsKey(CommandType.TalAttributes))
					talAttributesDictionary.Add(CommandType.TalAttributes, new List<TagAttribute>());
				talAttributesDictionary[CommandType.TalAttributes].InsertRange(0, cleanAttributes);
			}

			// Create a symbol for the end of the tag - we don't know what the offset is yet
			_endTagCommandLocationCounter++;

			TagStackItem tagStackItem = null;
			foreach (CommandType cmdType in talAttributesDictionary.Keys)
			{
				// Resolve program commands from tal attributes
				var commands = _talAttributeHandlers[cmdType](talAttributesDictionary[cmdType]);
				if (commands != null)
					foreach (Command cmd in commands)
					{
						if (tagStackItem == null)
						{
							// The first command needs to add the tag to the tag stack
							tagStackItem = AddTagToStack(tag, cleanAttributes);

							// Save metal:use-macro command position
							if (cmd.CommandType == CommandType.MetalUseMacro)
								tagStackItem.UseMacroCommandLocation = _programCommands.Count + 1;

							// Append command to create new scope for the tag
							Command startScopeCmd = new CmdStartScope(_currentStartTag);
							_programCommands.Add(startScopeCmd);
						}

						// All others just append
						_programCommands.Add(cmd);
					}
			}

			if (tagStackItem == null)
			{
				tagStackItem = AddTagToStack(tag, cleanAttributes);

				// Append command to create new scope for the tag
				_programCommands.Add(new CmdStartScope(_currentStartTag));
			}

			// Save pop functions and end tag command location for this tag
			tagStackItem.PopFunctionList = popFunctionList;
			tagStackItem.EndTagCommandLocation = _endTagCommandLocationCounter;

			// Finally, append start tag command
			_programCommands.Add(new CmdStartTag(_currentStartTag));
		}

		protected override void HandleEndTag(Tag tag)
		{
			while (_tagStack.Count > 0)
			{
				TagStackItem tagStackItem = _tagStack.Last();
				_tagStack.RemoveAt(_tagStack.Count - 1);

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
						_endTagsCommandMap[(int)endTagCommandLocation] = _programCommands.Count;

						// We need a "close scope and tag" command
						_programCommands.Add(new CmdEndTagEndScope(tag));
						return;
					}
					if (!tag.Singleton)
					{
						// We are popping off an un-interesting tag, just add the close as text
						_programCommands.Add(new CmdOutput(tag, "</" + tag.Name + ">"));
						return;
					}
					// We are suppressing the output of this tag, so just return
					return;
				}

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
			throw new TemplateParseException(null,
				string.Format("</{0}> {1}", tag.Name, "Close tag encountered with no corresponding open tag."));
		}

		protected override void HandleData(string data)
		{
			// Just add it as an output
			_programCommands.Add(new CmdOutput(_currentStartTag, data));
		}

		protected override void HandleComment(string data)
		{
			HandleData(data);
		}

		protected override void HandleCData(string data)
		{
			HandleData(data);
		}

		protected override void HandleProcessingInstruction(Element e)
		{
			var name = (e.StartTagTokens["name"]).ToString();
			var text = (e.StartTagTokens["text"]).ToString();
			if (name != "csharp")
			{
				HandleData(string.Format("<?{0}{1}?>", name, text));
				return;
			}
			_programCommands.Add(new CmdCodeBlock(name, text));
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
			var tagStackItem = new TagStackItem(tag);
			_tagStack.Add(tagStackItem);
			return tagStackItem;
		}

		void SetMetaPrefix(string prefix)
		{
			_metaNamespacePrefix = prefix;
			_metaAttributeMap = new Dictionary<string, CommandType>
			{
				{string.Format("{0}:interpolation", prefix), CommandType.MetaInterpolation}
			};
		}

		void SetTalPrefix(string prefix)
		{
			_talNamespacePrefix = prefix;
			_talAttributeMap = new Dictionary<string, CommandType>
			{
				{string.Format("{0}:attributes", prefix), CommandType.TalAttributes},
				{string.Format("{0}:content", prefix), CommandType.TalContent},
				{string.Format("{0}:define", prefix), CommandType.TalDefine},
				{string.Format("{0}:replace", prefix), CommandType.TalReplace},
				{string.Format("{0}:omit-tag", prefix), CommandType.TalOmittag},
				{string.Format("{0}:condition", prefix), CommandType.TalCondition},
				{string.Format("{0}:repeat", prefix), CommandType.TalRepeat}
			};
		}

		void SetMetalPrefix(string prefix)
		{
			_metalNamespacePrefix = prefix;
			_metalAttributeMap = new Dictionary<string, CommandType>
			{
				{string.Format("{0}:define-macro", prefix), CommandType.MetalDefineMacro},
				{string.Format("{0}:use-macro", prefix), CommandType.MetalUseMacro},
				{string.Format("{0}:define-slot", prefix), CommandType.MetalDefineSlot},
				{string.Format("{0}:fill-slot", prefix), CommandType.MetalFillSlot},
				{string.Format("{0}:define-param", prefix), CommandType.MetalDefineParam},
				{string.Format("{0}:fill-param", prefix), CommandType.MetalFillParam},
				{string.Format("{0}:import", prefix), CommandType.MetalImport}
			};
		}

		void PopMetaNamespace()
		{
			string newPrefix = _metaNamespacePrefixStack[_metaNamespacePrefixStack.Count - 1];
			_metaNamespacePrefixStack.RemoveAt(_metaNamespacePrefixStack.Count - 1);
			SetMetaPrefix(newPrefix);
		}

		void PopTalNamespace()
		{
			string newPrefix = _talNamespacePrefixStack[_talNamespacePrefixStack.Count - 1];
			_talNamespacePrefixStack.RemoveAt(_talNamespacePrefixStack.Count - 1);
			SetTalPrefix(newPrefix);
		}

		void PopMetalNamespace()
		{
			string newPrefix = _metalNamespacePrefixStack[_metalNamespacePrefixStack.Count - 1];
			_metalNamespacePrefixStack.RemoveAt(_metalNamespacePrefixStack.Count - 1);
			SetMetalPrefix(newPrefix);
		}

		List<Command> Handle_META_INTERPOLATION(List<TagAttribute> attributes)
		{
			// Only last declared attribute is valid
			string argument = attributes[attributes.Count - 1].Value;

			if (string.IsNullOrEmpty(argument))
			{
				// No argument passed
				const string msg = "No argument passed! meta:interpolation command must be of the form: meta:interpolation='true|false'";
				throw new TemplateParseException(_currentStartTag, msg);
			}

			if (argument == "true")
				return new List<Command> { new MetaInterpolation(_currentStartTag, true) };

			if (argument == "false")
				return new List<Command> { new MetaInterpolation(_currentStartTag, false) };

			throw new TemplateParseException(_currentStartTag,
				string.Format("Invalid command value '{0}'. Command meta:interpolation must be of the form: meta:interpolation='true|false'", argument));
		}

		List<Command> Handle_METAL_DEFINE_MACRO(List<TagAttribute> attributes)
		{
			// Only last declared attribute is valid
			string macroName = attributes[attributes.Count - 1].Value;

			if (string.IsNullOrEmpty(macroName))
			{
				// No argument passed
				const string msg = "No argument passed!  define-macro commands must be of the form: 'define-macro: name'";
				throw new TemplateParseException(_currentStartTag, msg);
			}

			// Check that the name of the macro is valid
			if (MetalNameRegex.Match(macroName).Length != macroName.Length)
			{
				string msg = string.Format("Macro name {0} is invalid.", macroName);
				throw new TemplateParseException(_currentStartTag, msg);
			}
			if (_macroMap.ContainsKey(macroName))
			{
				string msg = string.Format("Macro name {0} is already defined!", macroName);
				throw new TemplateParseException(_currentStartTag, msg);
			}

			// The macro starts at the next command.
			IProgram macro = new ProgramMacro(macroName, _programCommands.Count, _endTagCommandLocationCounter);
			_macroMap.Add(macroName, macro);

			return null;
		}

		List<Command> Handle_METAL_USE_MACRO(List<TagAttribute> attributes)
		{
			// Only last declared attribute is valid
			string argument = attributes[attributes.Count - 1].Value;

			// Sanity check
			if (argument.Length == 0)
			{
				// No argument passed
				const string msg = "No argument passed!  use-macro commands must be of the form: 'use-macro: path'";
				throw new TemplateParseException(_currentStartTag, msg);
			}

			return new List<Command> { new MetalUseMacro(_currentStartTag, argument, new Dictionary<string, ProgramSlot>(), new List<MetalDefineParam>()) };
		}

		List<Command> Handle_METAL_DEFINE_SLOT(List<TagAttribute> attributes)
		{
			// Only last declared attribute is valid
			string slotName = attributes[attributes.Count - 1].Value;

			// Compile a define-slot command.

			if (slotName.Length == 0)
			{
				// No argument passed
				const string msg = "No argument passed!  define-slot commands must be of the form: 'name'";
				throw new TemplateParseException(_currentStartTag, msg);
			}

			// Check that the name of the slot is valid
			if (MetalNameRegex.Match(slotName).Length != slotName.Length)
			{
				string msg = string.Format("Slot name {0} is invalid.", slotName);
				throw new TemplateParseException(_currentStartTag, msg);
			}

			return new List<Command> { new MetalDefineSlot(_currentStartTag, slotName) };
		}

		List<Command> Handle_METAL_FILL_SLOT(List<TagAttribute> attributes)
		{
			// Only last declared attribute is valid
			string slotName = attributes[attributes.Count - 1].Value;

			if (slotName.Length == 0)
			{
				// No argument passed
				const string msg = "No argument passed!  fill-slot commands must be of the form: 'fill-slot: name'";
				throw new TemplateParseException(_currentStartTag, msg);
			}

			// Check that the name of the slot is valid
			if (MetalNameRegex.Match(slotName).Length != slotName.Length)
			{
				string msg = string.Format("Slot name {0} is invalid.", slotName);
				throw new TemplateParseException(_currentStartTag, msg);
			}

			// Determine what use-macro statement this belongs to by working through the list backwards
			int ourMacroLocation = -1;
			int location = _tagStack.Count - 1;
			while (ourMacroLocation == -1)
			{
				int macroLocation = _tagStack[location].UseMacroCommandLocation;
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
						throw new TemplateParseException(_currentStartTag, msg);
					}
				}
			}

			// Update the metal:use-macro command slot definitions
			var useMacroCmd = (MetalUseMacro)_programCommands[ourMacroLocation];
			if (useMacroCmd.Slots.ContainsKey(slotName))
			{
				string msg = string.Format("Slot {0} has already been filled!", slotName);
				throw new TemplateParseException(_currentStartTag, msg);
			}
			int slotCommandStart = _programCommands.Count; // The slot starts at the next command.
			var slot = new ProgramSlot(slotName, slotCommandStart, _endTagCommandLocationCounter);
			useMacroCmd.Slots.Add(slotName, slot);

			return null;
		}

		List<Command> Handle_METAL_DEFINE_PARAM(List<TagAttribute> attributes)
		{
			// Join attributes for commands that support multiple attributes
			string argument = string.Join(";", attributes.Select(a => a.Value).ToArray());

			var commands = new List<Command>();

			// We only want to match semi-colons that are not escaped
			foreach (string defStmt in MetalDefineParamRegex.Split(argument))
			{
				//  remove any leading space and un-escape any semi-colons
				string defineStmt = defStmt.TrimStart().Replace(";;", ";");

				// Break each defineStmt into pieces "[local|global] varName expression"
				var stmtBits = new List<string>(defineStmt.Split(new[] { ' ' }));
				string expression = "";
				if (stmtBits.Count < 2)
				{
					// Error, badly formed define-param command
					string msg = string.Format("Badly formed define-param command '{0}'.  Define commands must be of the form: 'varType varName [expression][;varType varName [expression]]'", argument);
					throw new TemplateParseException(_currentStartTag, msg);
				}
				string varType = stmtBits[0];
				string varName = stmtBits[1];
				if (stmtBits.Count >= 3)
					expression = string.Join(" ", stmtBits.GetRange(2, stmtBits.Count - 2).ToArray());

				commands.Add(new MetalDefineParam(_currentStartTag, varType, varName, expression));
			}

			return commands;
		}

		List<Command> Handle_METAL_FILL_PARAM(List<TagAttribute> attributes)
		{
			// Join attributes for commands that support multiple attributes
			string argument = string.Join(";", attributes.Select(a => a.Value).ToArray());

			var fillParamsCommands = new List<MetalDefineParam>();

			// We only want to match semi-colons that are not escaped
			foreach (string defStmt in MetalFillParamRegex.Split(argument))
			{
				//  remove any leading space and un-escape any semi-colons
				string defineStmt = defStmt.TrimStart().Replace(";;", ";");

				// Break each defineStmt into pieces "[local|global] varName expression"
				var stmtBits = new List<string>(defineStmt.Split(new[] { ' ' }));
				if (stmtBits.Count < 2)
				{
					// Error, badly formed fill-param command
					string msg = string.Format("Badly formed fill-param command '{0}'.  Fill-param commands must be of the form: 'varName expression[;varName expression]'", argument);
					throw new TemplateParseException(_currentStartTag, msg);
				}
				string varName = stmtBits[0];
				string expression = string.Join(" ", stmtBits.GetRange(1, stmtBits.Count - 1).ToArray());

				fillParamsCommands.Add(new MetalDefineParam(_currentStartTag, "", varName, expression));
			}

			// Determine what metal:use-macro statement this belongs to by working through the list backwards
			int ourMacroLocation = -1;
			int stackIndex = _tagStack.Count - 1;
			while (ourMacroLocation == -1)
			{
				int macroLocation = _tagStack[stackIndex].UseMacroCommandLocation;
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
						throw new TemplateParseException(_currentStartTag, msg);
					}
				}
			}

			// Update the metal:use-macro command param definitions
			var useMacroCmd = (MetalUseMacro)_programCommands[ourMacroLocation];
			useMacroCmd.Parameters.AddRange(fillParamsCommands);

			return null;
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
				const string msg = "No argument passed! Metal import commands must be of the form: 'path'";
				throw new TemplateParseException(_currentStartTag, msg);
			}

			// Break up the list of imports first
			// We only want to match semi-colons that are not escaped
			foreach (string impStmt in MetalImportRegex.Split(argument))
			{
				//  remove any leading space and un-escape any semi-colons
				string importStmt = impStmt.Trim().Replace(";;", ";");
				string importNs = MainProgramNamespace;
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
					var stmtBits = new List<string>(importStmt.Split(new[] { ':' }, 2));
					if (stmtBits.Count < 1)
					{
						// Error, badly formed import command
						string msg = string.Format("Badly formed import command '{0}'.  Import commands must be of the form: '(importNs:)importPath[;(importNs:)importPath]'", argument);
						throw new TemplateParseException(_currentStartTag, msg);
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
						throw new TemplateParseException(_currentStartTag, msg);
					}
				}

				// Save import command to cache
				string importCmd = string.Format("{0}:{1}", importNs, importPath);
				if (!_importMacroCommands.Contains(importCmd))
				{
					_importMacroCommands.Add(importCmd);
				}
			}

			return null;
		}

		List<Command> Handle_TAL_DEFINE(List<TagAttribute> attributes)
		{
			// Join attributes for commands that support multiple attributes
			string argument = string.Join(";", attributes.Select(a => a.Value).ToArray());

			var commands = new List<Command>();

			// We only want to match semi-colons that are not escaped
			foreach (string defStmt in TalDefineRegex.Split(argument))
			{
				//  remove any leading space and un-escape any semi-colons
				string defineStmt = defStmt.TrimStart().Replace(";;", ";");

				// Break each defineStmt into pieces "[local|global] varName expression"
				var stmtBits = new List<string>(defineStmt.Split(new[] { ' ' }));
				var varScope = TalDefine.VariableScope.Local;
				string varName;
				string expression;
				if (stmtBits.Count < 2)
				{
					// Error, badly formed define command
					string msg = string.Format("Badly formed define command '{0}'.  Define commands must be of the form: '[local|nonlocal|global] varName expression[;[local|nonlocal|global] varName expression]'", argument);
					throw new TemplateParseException(_currentStartTag, msg);
				}
				// Assume to start with that >2 elements means a local|global flag
				if (stmtBits.Count > 2)
				{
					if (stmtBits[0] == "global")
					{
						varScope = TalDefine.VariableScope.Global;
						varName = stmtBits[1];
						expression = string.Join(" ", stmtBits.GetRange(2, stmtBits.Count - 2).ToArray());
					}
					else if (stmtBits[0] == "local")
					{
						varScope = TalDefine.VariableScope.Local;
						varName = stmtBits[1];
						expression = string.Join(" ", stmtBits.GetRange(2, stmtBits.Count - 2).ToArray());
					}
					else if (stmtBits[0] == "nonlocal")
					{
						varScope = TalDefine.VariableScope.NonLocal;
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

				commands.Add(new TalDefine(_currentStartTag, varScope, varName, expression));
			}

			return commands;
		}

		List<Command> Handle_TAL_CONDITION(List<TagAttribute> attributes)
		{
			// Only last declared attribute is valid
			string expression = attributes[attributes.Count - 1].Value;

			// Sanity check
			if (expression.Length == 0)
			{
				// No argument passed
				const string msg = "No argument passed!  condition commands must be of the form: 'path'";
				throw new TemplateParseException(_currentStartTag, msg);
			}

			return new List<Command> { new TalCondition(_currentStartTag, expression) };
		}

		List<Command> Handle_TAL_REPEAT(List<TagAttribute> attributes)
		{
			// Only last declared attribute is valid
			string argument = attributes[attributes.Count - 1].Value;

			var attProps = new List<string>(argument.Split(new[] { ' ' }));

			// Sanity check
			if (attProps.Count < 2)
			{
				// Error, badly formed repeat command
				string msg = string.Format("Badly formed repeat command '{0}'.  Repeat commands must be of the form: 'variable path'", argument);
				throw new TemplateParseException(_currentStartTag, msg);
			}

			string varName = attProps[0];
			string expression = string.Join(" ", attProps.GetRange(1, attProps.Count - 1).ToArray());

			return new List<Command> { new TalRepeat(_currentStartTag, varName, expression) };
		}

		List<Command> Handle_TAL_CONTENT(List<TagAttribute> attributes)
		{
			return Handle_TAL_CONTENT(attributes, false);
		}

		List<Command> Handle_TAL_CONTENT(List<TagAttribute> attributes, bool replace)
		{
			// Only last declared attribute is valid
			string argument = attributes[attributes.Count - 1].Value;

			// Compile a content or replace command

			// Sanity check
			if (argument.Length == 0)
			{
				// No argument passed
				const string msg = "No argument passed!  content/replace commands must be of the form: 'path'";
				throw new TemplateParseException(_currentStartTag, msg);
			}

			bool structure = false;
			string expression;

			string[] attProps = argument.Split(new[] { ' ' });
			if (attProps.Length > 1)
			{
				if (attProps[0] == "structure")
				{
					structure = true;
					expression = string.Join(" ", attProps, 1, attProps.Length - 1);
				}
				else if (attProps[0] == "text")
				{
					expression = string.Join(" ", attProps, 1, attProps.Length - 1);
				}
				else
				{
					// It's not a type selection after all - assume it's part of the expression
					expression = argument;
				}
			}
			else
				expression = argument;

			if (replace)
				return new List<Command> { new TalReplace(_currentStartTag, expression, structure) };

			return new List<Command> { new TalContent(_currentStartTag, expression, structure) };
		}

		List<Command> Handle_TAL_REPLACE(List<TagAttribute> attributes)
		{
			return Handle_TAL_CONTENT(attributes, true);
		}

		List<Command> Handle_TAL_ATTRIBUTES(List<TagAttribute> attributes)
		{
			// Compile tal:attributes into attribute command

			var attrList = new List<TagAttribute>();
			foreach (TagAttribute att in attributes)
			{
				var attribute = att as TalTagAttribute;
				if (attribute != null)
				{
					// This is TAL command attribute
					// Break up the attribute args to list of TALTagAttributes
					// We only want to match semi-colons that are not escaped
					foreach (string attStmt in TalAttributesRegex.Split(attribute.Value))
					{
						// Remove any leading space and un-escape any semi-colons
						// Break each attributeStmt into name and expression
						var stmtBits = new List<string>(attStmt.TrimStart().Replace(";;", ";").Split(' '));
						if (stmtBits.Count < 2)
						{
							// Error, badly formed attributes command
							string msg = string.Format(
								"Badly formed attributes command '{0}'. Attributes commands must be of the form: 'name expression[;name expression]'",
								attribute.Value);
							throw new TemplateParseException(_currentStartTag, msg);
						}
						var talTagAttr = new TalTagAttribute
						{
							CommandType = attribute.CommandType,
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

			var cmd = new TalAttributes(_currentStartTag, attrList);

			return new List<Command> { cmd };
		}

		List<Command> Handle_TAL_OMITTAG(List<TagAttribute> attributes)
		{
			// Only last declared attribute is valid
			string argument = attributes[attributes.Count - 1].Value;

			// Compile a condition command.
			// If no argument is given then set the path to default

			string expression;
			if (argument.Length == 0)
				expression = DefaultValueExpression;
			else
				expression = argument;

			return new List<Command> { new TalOmitTag(_currentStartTag, expression) };
		}
	}
}
