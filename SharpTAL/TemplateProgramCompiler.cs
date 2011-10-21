//
// TemplateProgramCompiler.cs
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
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SharpTAL
{
	using SharpTAL.Parser;

	public class Tag
	{
		public string Name { get; set; }
		public string Suffix { get; set; }
		public List<Attr> Attributes { get; set; }
		public int LineNumber { get; set; }
		public int LinePosition { get; set; }
		public string SourcePath { get; set; }
		public bool OmitTagScope { get; set; }

		public Tag()
		{
			OmitTagScope = false;
		}

		public Tag(Tag tag)
		{
			Name = tag.Name;
			SourcePath = tag.SourcePath;
			LineNumber = tag.LineNumber;
			LinePosition = tag.LinePosition;
			Attributes = new List<Attr>(tag.Attributes);
			OmitTagScope = tag.OmitTagScope;
		}

		public string Format(bool escape = true)
		{
			string result = "<";
			result += Name;
			foreach (var att in Attributes)
			{
				result += string.Format(" {0}{1}{2}{3}{2}", att.Name, att.Eq, att.Quote, Utils.EscapeAttrValue(att));
			}
			result += Suffix;
			return result;
		}

		public override string ToString()
		{
			return Format(false);
		}
	}

	public class Attr
	{
		public CommandType? CommandType { get; set; }
		public string Name { get; set; }
		public string Value { get; set; }
		public string Eq { get; set; }
		public string Quote { get; set; }
		public string QuoteEntity { get; set; }
	}

	public class TagInfo
	{
		public Tag Tag { get; set; }
		public Dictionary<string, object> Properties { get; set; }
		public int UseMacroLocation { get; set; }
	}

	//-----------------------------------------------------------
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

	public enum CommandType
	{
		METAL_USE_MACRO = 1,
		METAL_DEFINE_SLOT = 2,
		METAL_FILL_SLOT = 3,
		METAL_DEFINE_MACRO = 4,
		METAL_DEFINE_PARAM = 5,
		METAL_FILL_PARAM = 6,
		METAL_IMPORT = 7,
		TAL_DEFINE = 10,
		TAL_CONDITION = 11,
		TAL_REPEAT = 12,
		TAL_CONTENT = 13,
		TAL_REPLACE = 14,
		TAL_ATTRIBUTES = 15,
		TAL_OMITTAG = 16,
		TAL_START_SCOPE = 17,
		TAL_OUTPUT = 18,
		TAL_STARTTAG = 19,
		TAL_ENDTAG_ENDSCOPE = 20,
		TAL_NOOP = 21
	}

	public class CommandComparer : IComparer<CommandType>
	{
		public int Compare(CommandType x, CommandType y)
		{
			// TAL/METAL commands are represented as integers, lower values have higher priority
			return ((int)x).CompareTo(((int)y));
		}
	}

	public class Command
	{
		Tag tag = null;

		public CommandType CommandType { get; set; }
		public List<object> Attributes { get; set; }

		public Tag Tag
		{
			set
			{
				if (value != null)
				{
					tag = new Tag();
					tag.Name = value.Name;
					tag.Attributes = new List<Attr>(value.Attributes);
					tag.SourcePath = value.SourcePath;
					tag.LineNumber = value.LineNumber;
					tag.LinePosition = value.LinePosition;
					tag.OmitTagScope = value.OmitTagScope;
				}
			}
			get
			{
				return tag;
			}
		}
	}

	public enum TALDefineAction
	{
		Local = 1,
		SetLocal = 2,
		Global = 3
	}

	public class TALDefineInfo
	{
		public TALDefineAction Action { get; set; }
		public string Type { get; set; }
		public string Name { get; set; }
		public string Expression { get; set; }
	}

	public class TemplateProgramCompiler
	{
		/// <summary>
		/// Contains compiled template programs. The key is the template body hash.
		/// </summary>
		static Dictionary<string, TemplateProgram> templateProgramCache = new Dictionary<string, TemplateProgram>();
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

		protected Dictionary<CommandType, Func<List<Attr>, Command>> commandHandler;

		// Per-template compiling state (including inline templates compiling)
		protected string tal_namespace_prefix;
		protected string tal_namespace_omittag;
		protected string tal_namespace_omitscope;
		protected List<string> tal_namespace_prefix_stack;
		protected Dictionary<string, CommandType> tal_attribute_map;
		protected string metal_namespace_prefix;
		protected List<string> metal_namespace_prefix_stack;
		protected Dictionary<string, CommandType> metal_attribute_map;

		// Per-template-body compiling state
		protected HashSet<string> importMacroCommands = null;
		protected List<Command> commandList;
		protected Dictionary<int, int> symbolLocationTable;
		protected Dictionary<string, TemplateProgramMacro> macroMap;
		protected List<TagInfo> tagStack;
		protected int endTagSymbol;
		protected Tag currentStartTag;

		public TemplateProgramCompiler()
		{
			commandHandler = new Dictionary<CommandType, Func<List<Attr>, Command>>();
			commandHandler.Add(CommandType.METAL_USE_MACRO, Compile_METAL_USE_MACRO);
			commandHandler.Add(CommandType.METAL_DEFINE_SLOT, Compile_METAL_DEFINE_SLOT);
			commandHandler.Add(CommandType.METAL_FILL_SLOT, Compile_METAL_FILL_SLOT);
			commandHandler.Add(CommandType.METAL_DEFINE_MACRO, Compile_METAL_DEFINE_MACRO);
			commandHandler.Add(CommandType.METAL_DEFINE_PARAM, Compile_METAL_DEFINE_PARAM);
			commandHandler.Add(CommandType.METAL_FILL_PARAM, Compile_METAL_FILL_PARAM);
			commandHandler.Add(CommandType.METAL_IMPORT, Compile_METAL_IMPORT);
			commandHandler.Add(CommandType.TAL_DEFINE, Compile_TAL_DEFINE);
			commandHandler.Add(CommandType.TAL_CONDITION, Compile_TAL_CONDITION);
			commandHandler.Add(CommandType.TAL_REPEAT, Compile_TAL_REPEAT);
			commandHandler.Add(CommandType.TAL_CONTENT, Compile_TAL_CONTENT);
			commandHandler.Add(CommandType.TAL_REPLACE, Compile_TAL_REPLACE);
			commandHandler.Add(CommandType.TAL_ATTRIBUTES, Compile_TAL_ATTRIBUTES);
			commandHandler.Add(CommandType.TAL_OMITTAG, Compile_TAL_OMITTAG);
		}

		public void CompileTemplate(ref TemplateInfo ti)
		{
			// Init per-template compiling state (including inline templates compiling)
			// Default namespaces
			SetTALPrefix("tal");
			tal_namespace_prefix_stack = new List<string>();
			tal_namespace_prefix_stack.Add("tal");
			SetMETALPrefix("metal");
			metal_namespace_prefix_stack = new List<string>();
			metal_namespace_prefix_stack.Add("metal");

			ti.ImportedPrograms = new Dictionary<string, TemplateProgram>();
			ti.ImportedNamespaces = new Dictionary<string, HashSet<string>>();

			// Compile main template body
			ti.MainProgram = GetTemplateProgram(ti.TemplateBody, MAIN_TEMPLATE_PATH);

			// Compile imported templates
			CompileImportedTemplates(ti, ti.MainProgram);
		}

		private void CompileImportedTemplates(TemplateInfo ti, TemplateProgram program)
		{
			foreach (string importCmd in program.ImportCommands)
			{
				// Parse import command
				string programNamespace = importCmd.Split(new char[] { ':' }, 2)[0];
				string templatePath = importCmd.Split(new char[] { ':' }, 2)[1];

				TemplateProgram importedProgram;
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

		private TemplateProgram GetTemplateProgram(string templateBody, string templatePath)
		{
			// Init per-template-body compiling state
			importMacroCommands = new HashSet<string>();

			// Try to get template program from cache
			string bodyHash = Utils.ComputeHash(templateBody);
			TemplateProgram program = null;
			lock (templateProgramCacheLock)
			{
				if (templateProgramCache.TryGetValue(bodyHash, out program))
					return program;
			}
			if (program != null)
				return program;

			// Per-template-body compiling state
			commandList = new List<Command>();
			symbolLocationTable = new Dictionary<int, int>();
			macroMap = new Dictionary<string, TemplateProgramMacro>();
			tagStack = new List<TagInfo>();
			endTagSymbol = 1;
			currentStartTag = null;

			// Compile template program from template body
			IEnumerable<Token> tokens = Tokenizer.TokenizeXml(templateBody, templatePath);
			ElementParser parser = new ElementParser(tokens, new Dictionary<string, string> {
				{ "xmlns", Namespaces.XMLNS_NS },
				{ "xml", Namespaces.XML_NS },
				{ "tal", Namespaces.TAL_NS },
				{ "metal", Namespaces.METAL_NS } });
			foreach (var e in parser.Parse())
			{
				CompileElement(e);
			}
			program = new TemplateProgram(templateBody, templatePath, bodyHash, commandList, symbolLocationTable, macroMap, importMacroCommands);

			// Put template program to cache
			lock (templateProgramCacheLock)
			{
				if (!templateProgramCache.ContainsKey(bodyHash))
					templateProgramCache.Add(bodyHash, program);
			}

			return program;
		}

		private void CompileElement(Element e)
		{
			if (e.Kind == ElementKind.Element || e.Kind == ElementKind.StartTag)
			{
				// Start tag
				Token name = e.StartTagTokens["name"] as Token;
				Token suffix = e.StartTagTokens["suffix"] as Token;
				Tag tag = new Tag();
				tag.Name = name.ToString();
				tag.Suffix = suffix.ToString();
				tag.SourcePath = name.Filename;
				Location loc = name.Location;
				tag.LineNumber = loc.Line;
				tag.LinePosition = loc.Position;
				tag.Attributes = new List<Attr>();
				List<Dictionary<string, object>> attrs = e.StartTagTokens["attrs"] as List<Dictionary<string, object>>;
				foreach (var attr in attrs)
				{
					Token attr_name = attr["name"] as Token;
					Token attr_value = attr["value"] as Token;
					Token attr_eq = attr["eq"] as Token;
					Token attr_quote = attr["quote"] as Token;
					Attr a = new Attr
					{
						Name = attr_name.ToString(),
						Value = attr_value.ToString(),
						Eq = attr_eq.ToString(),
						Quote = attr_quote.ToString(),
						QuoteEntity = Utils.Char2Entity(attr_quote.ToString())
					};
					tag.Attributes.Add(a);
				}
				if ((e.Children.Count == 0 && suffix.ToString() == "/>") || e.EndTagTokens.Count == 0)
					// Singleton element
					CompileStartEndTag(tag);
				else
					CompileStartTag(tag);
				// Children
				foreach (var item in e.Children)
				{
					CompileElement(item);
				}
				// End tag
				if (e.EndTagTokens.Count > 0)
				{
					Token end_name = e.EndTagTokens["name"] as Token;
					Token end_suffix = e.EndTagTokens["suffix"] as Token;
					Tag end_tag = new Tag();
					end_tag.Name = end_name.ToString();
					end_tag.Suffix = end_suffix.ToString();
					end_tag.SourcePath = end_name.Filename;
					Location end_loc = end_name.Location;
					end_tag.LineNumber = end_loc.Line;
					end_tag.LinePosition = end_loc.Position;
					CompileEndTag(end_tag);
				}
			}
			else if (e.Kind == ElementKind.Text)
			{
				foreach (Token token in e.StartTagTokens.Values)
				{
					CompileData(token.ToString());
				}
			}
			else if (e.Kind == ElementKind.Comment)
			{
				foreach (Token token in e.StartTagTokens.Values)
				{
					CompileComment(token.ToString());
				}
			}
			else if (e.Kind == ElementKind.Default)
			{
				foreach (Token token in e.StartTagTokens.Values)
				{
					CompileDefault(token.ToString());
				}
			}
		}

		protected void CompileStartEndTag(Tag tag)
		{
			CompileStartTag(tag, 1);
			CompileEndTag(tag);
		}

		protected void CompileStartTag(Tag tag, int singletonElement = 0)
		{
			// Note down the tag we are handling, it will be used for error handling during compilation
			currentStartTag = new Tag(tag);

			// Expand HTML entity references in attribute values
			foreach (Attr att in currentStartTag.Attributes)
				att.Value = Utils.Unescape(att.Value);

			// Look for TAL/METAL attributes
			SortedDictionary<CommandType, List<Attr>> commands = new SortedDictionary<CommandType, List<Attr>>(new CommandComparer());
			List<Attr> cleanAttributes = new List<Attr>();
			Dictionary<string, object> tagProperties = new Dictionary<string, object>();
			List<Action> popTagFuncList = new List<Action>();
			bool isTALElementNameSpace = false;
			string prefixToAdd = "";
			tagProperties.Add("singletonTag", singletonElement);

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
					// This tag has not his own scope
					if (tag.Name == tal_namespace_omitscope)
					{
						tag.OmitTagScope = true;
						currentStartTag.OmitTagScope = true;
					}
					isTALElementNameSpace = true;
					prefixToAdd = tal_namespace_prefix + ":";
				}
				if (isTALElementNameSpace)
				{
					// We should treat this an implicit omit-tag
					// Will go to default, i.e. yes
					commands[CommandType.TAL_OMITTAG] = new List<Attr>() { new Attr { Value = "", CommandType = CommandType.TAL_OMITTAG } };
				}
			}

			// Resolve TAL/METAL commands from attributes
			foreach (var att in currentStartTag.Attributes)
			{
				string commandName = "";

				if (isTALElementNameSpace && att.Name.IndexOf(':') < 0)
					// This means that the attribute name does not have a namespace, so use the prefix for this tag.
					commandName = prefixToAdd + att.Name;
				else
					commandName = att.Name;

				if (att.Name.Length > 4 && att.Name.Substring(0, 5) == "xmlns")
				{
					// We have a namespace declaration.
					string prefix = att.Name.Length > 5 ? att.Name.Substring(6) : "";
					if (att.Value == Namespaces.METAL_NS)
					{
						// It's a METAL namespace declaration
						if (prefix.Length > 0)
						{
							metal_namespace_prefix_stack.Add(metal_namespace_prefix);
							SetMETALPrefix(prefix);
							// We want this function called when the scope ends
							popTagFuncList.Add(PopMETALNamespace);
						}
						else
						{
							// We don't allow METAL/TAL to be declared as a default
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
							popTagFuncList.Add(PopTALNamespace);
						}
						else
						{
							// We don't allow METAL/TAL to be declared as a default
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
				else if (tal_attribute_map.ContainsKey(commandName))
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
							commands.Add(cmdType, new List<Attr>());
						att.CommandType = cmdType;
						commands[cmdType].Add(att);
					}
				}
				else if (metal_attribute_map.ContainsKey(commandName))
				{
					// It's a METAL attribute
					CommandType cmdType = metal_attribute_map[commandName];
					if (!commands.ContainsKey(cmdType))
						commands.Add(cmdType, new List<Attr>());
					att.CommandType = cmdType;
					commands[cmdType].Add(att);
				}
				else
				{
					// It's normal HTML/XML attribute
					cleanAttributes.Add(att);
				}
			}
			tagProperties.Add("popFunctionList", popTagFuncList);

			if (cleanAttributes.Count > 0)
			{
				// Insert normal attributes BEFORE other TAL/METAL attributes of type TAL_ATTRIBUTES
				// as fake TAL_ATTRIBUTES to enable string expressions processing.
				if (!commands.ContainsKey(CommandType.TAL_ATTRIBUTES))
					commands.Add(CommandType.TAL_ATTRIBUTES, new List<Attr>());
				commands[CommandType.TAL_ATTRIBUTES].InsertRange(0, cleanAttributes);
			}

			// This might be just content
			if (commands.Count == 0)
			{
				// Just content, add it to the various stacks
				AddTag(tag, cleanAttributes, tagProperties);
				return;
			}

			// Create a symbol for the end of the tag - we don't know what the offset is yet
			endTagSymbol += 1;
			tagProperties.Add("endTagSymbol", endTagSymbol);

			int firstTag = 1;
			foreach (CommandType cmdType in commands.Keys)
			{
				// Create command from attributes
				Command cmnd = commandHandler[cmdType](commands[cmdType]);
				if (cmnd != null)
				{
					if (firstTag == 1)
					{
						// The first one needs to add the tag
						firstTag = 0;
						tagProperties["command"] = cmnd;
						AddTag(tag, cleanAttributes, tagProperties);
					}
					else
					{
						// All others just append
						AddCommand(cmnd);
					}
				}
			}

			Command cmd = new Command();
			cmd.Tag = currentStartTag;
			cmd.CommandType = CommandType.TAL_STARTTAG;
			cmd.Attributes = new List<object>();
			cmd.Attributes.Add(tag);
			cmd.Attributes.Add(singletonElement);

			if (firstTag == 1)
			{
				tagProperties["command"] = cmd;
				AddTag(tag, cleanAttributes, tagProperties);
			}
			else
			{
				// Add the start tag command in as a child of the last TAL command
				AddCommand(cmd);
			}
		}

		protected void CompileEndTag(Tag tag)
		{
			while (tagStack.Count > 0)
			{
				TagInfo ti = tagStack[tagStack.Count - 1];
				tagStack.RemoveAt(tagStack.Count - 1);

				Tag oldTag = ti.Tag;
				Dictionary<string, object> tagProperties = ti.Properties;

				int? endTagSymbol = (int?)(tagProperties.ContainsKey("endTagSymbol") ? tagProperties["endTagSymbol"] : null);
				List<Action> popCommandList = (List<Action>)(tagProperties.ContainsKey("popCommandList") ? tagProperties["popCommandList"] : null);
				int singletonTag = (int)(tagProperties.ContainsKey("singletonTag") ? tagProperties["singletonTag"] : 0);

				if (popCommandList != null)
				{
					foreach (Action func in popCommandList)
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
						symbolLocationTable[(int)endTagSymbol] = commandList.Count;

						// We need a "close scope and tag" command
						Command cmd = new Command();
						cmd.Tag = currentStartTag;
						cmd.CommandType = CommandType.TAL_ENDTAG_ENDSCOPE;
						cmd.Attributes = new List<object>();
						cmd.Attributes.Add(tag.Name);
						cmd.Attributes.Add(singletonTag);
						AddCommand(cmd);
						return;
					}
					else if (singletonTag == 0)
					{
						// We are popping off an un-interesting tag, just add the close as text
						// We need a "close scope and tag" command
						Command cmd = new Command();
						cmd.Tag = currentStartTag;
						cmd.CommandType = CommandType.TAL_OUTPUT;
						cmd.Attributes = new List<object>();
						cmd.Attributes.Add("</" + tag.Name + ">");
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

		protected void CompileData(string data)
		{
			// Just add it as an output
			Command cmd = new Command();
			cmd.Tag = currentStartTag;
			cmd.CommandType = CommandType.TAL_OUTPUT;
			cmd.Attributes = new List<object>();
			cmd.Attributes.Add(data);
			AddCommand(cmd);
		}

		protected void CompileComment(string data)
		{
			CompileData(data);
		}

		protected void CompileDefault(string data)
		{
			CompileData(data);
		}

		protected void AddTag(Tag tag, List<Attr> cleanAttributes, Dictionary<string, object> tagProperties)
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

			Command command = (Command)(tagProperties.ContainsKey("command") ? tagProperties["command"] : null);
			Dictionary<string, Attr> originalAtts = (Dictionary<string, Attr>)(tagProperties.ContainsKey("originalAtts") ? tagProperties["originalAtts"] : null);
			int singletonTag = (int)(tagProperties.ContainsKey("singletonTag") ? tagProperties["singletonTag"] : 0);

			TagInfo ti = new TagInfo();
			if (command != null)
			{
				if (command.CommandType == CommandType.METAL_USE_MACRO)
				{
					ti.Tag = tag;
					ti.Properties = tagProperties;
					ti.UseMacroLocation = commandList.Count + 1;
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
			tagStack.Add(ti);

			if (command != null)
			{
				// All tags that have a TAL attribute on them start with a 'start scope'
				Command cmd = new Command();
				cmd.Tag = currentStartTag;
				cmd.CommandType = CommandType.TAL_START_SCOPE;
				cmd.Attributes = new List<object>();
				AddCommand(cmd);
				// Now we add the TAL command
				AddCommand(command);
			}
			else
			{
				// It's just a straight output, so create an output command and append it
				Command cmd = new Command();
				cmd.Tag = currentStartTag;
				cmd.CommandType = CommandType.TAL_OUTPUT;
				cmd.Attributes = new List<object>();
				cmd.Attributes.Add(tag.Format());
				AddCommand(cmd);
			}
		}

		protected void AddCommand(Command command)
		{
			//
			// Only following commands can be used inside Tags without scope:
			//	TAL_DEFINE, TAL_OUTPUT
			// Following commands are ignored by the "SourceGenerator" class:
			//	TAL_START_SCOPE, TAL_STARTTAG, TAL_ENDTAG_ENDSCOPE, TAL_OMITTAG
			//
			if (command.Tag != null &&
				command.Tag.OmitTagScope == true &&
				command.CommandType != CommandType.TAL_DEFINE &&
				command.CommandType != CommandType.TAL_OUTPUT &&
				command.CommandType != CommandType.TAL_START_SCOPE &&
				command.CommandType != CommandType.TAL_STARTTAG &&
				command.CommandType != CommandType.TAL_ENDTAG_ENDSCOPE &&
				command.CommandType != CommandType.TAL_OMITTAG)
			{
				// This command can not be used inside tag without his own scope
				string msg = string.Format(@"Command ""{0}"" can not be used inside tag <tal:omit-scope>'.",
					Enum.GetName(typeof(CommandType), command.CommandType));
				throw new TemplateParseException(currentStartTag, msg);
			}

			if (command.CommandType == CommandType.TAL_OUTPUT &&
				commandList.Count > 0 &&
				commandList[commandList.Count - 1].CommandType == CommandType.TAL_OUTPUT)
			{
				// We can combine output commands
				Command cmd = commandList[commandList.Count - 1];
				foreach (object att in command.Attributes)
					cmd.Attributes.Add(att);
			}
			else
			{
				commandList.Add(command);
			}
		}

		protected Command Compile_TAL_DEFINE(List<Attr> attributes)
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
					string msg = string.Format("Badly formed define command '{0}'.  Define commands must be of the form: '[local|global] varName expression[;[local|global] varName expression]'", argument);
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
					else if (stmtBits[0] == "set")
					{
						defAction = TALDefineAction.SetLocal;
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

			Command ci = new Command();
			ci.Tag = currentStartTag;
			ci.CommandType = CommandType.TAL_DEFINE;
			ci.Attributes = new List<object>
			{
				commandArgs
			};
			return ci;
		}

		protected Command Compile_TAL_CONDITION(List<Attr> attributes)
		{
			// Only last declared attribute is valid
			string argument = attributes[attributes.Count - 1].Value;

			// Compile a condition command, resulting argument is:
			// path, endTagSymbol
			// Sanity check
			if (argument.Length == 0)
			{
				// No argument passed
				string msg = "No argument passed!  condition commands must be of the form: 'path'";
				throw new TemplateParseException(currentStartTag, msg);
			}

			Command ci = new Command();
			ci.Tag = currentStartTag;
			ci.CommandType = CommandType.TAL_CONDITION;
			ci.Attributes = new List<object>
			{
				argument,
				endTagSymbol
			};
			return ci;
		}

		protected Command Compile_TAL_REPEAT(List<Attr> attributes)
		{
			// Only last declared attribute is valid
			string argument = attributes[attributes.Count - 1].Value;

			// Compile a repeat command, resulting argument is:
			// (varname, expression, endTagSymbol)
			List<string> attProps = new List<string>(argument.Split(new char[] { ' ' }));
			if (attProps.Count < 2)
			{
				// Error, badly formed repeat command
				string msg = string.Format("Badly formed repeat command '{0}'.  Repeat commands must be of the form: 'localVariable path'", argument);
				throw new TemplateParseException(currentStartTag, msg);
			}

			string varName = attProps[0];
			string expression = string.Join(" ", attProps.GetRange(1, attProps.Count - 1).ToArray());

			Command ci = new Command();
			ci.Tag = currentStartTag;
			ci.CommandType = CommandType.TAL_REPEAT;
			ci.Attributes = new List<object>
			{
				varName,
				expression,
				endTagSymbol
			};
			return ci;
		}

		protected Command Compile_TAL_CONTENT(List<Attr> attributes)
		{
			return Compile_TAL_CONTENT(attributes, 0);
		}

		protected Command Compile_TAL_CONTENT(List<Attr> attributes, int replaceFlag)
		{
			// Only last declared attribute is valid
			string argument = attributes[attributes.Count - 1].Value;

			// Compile a content command, resulting argument is
			// (replaceFlag, structureFlag, expression, endTagSymbol)

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

			Command ci = new Command();
			ci.Tag = currentStartTag;
			ci.CommandType = CommandType.TAL_CONTENT;
			ci.Attributes = new List<object>
			{
				replaceFlag,
				structureFlag,
				express,
				endTagSymbol
			};
			return ci;
		}

		protected Command Compile_TAL_REPLACE(List<Attr> attributes)
		{
			return Compile_TAL_CONTENT(attributes, 1);
		}

		protected Command Compile_TAL_ATTRIBUTES(List<Attr> attributes)
		{
			// Compile tal:attributes into attribute command
			// Argument: [(attributeName, expression)]

			List<Attr> commandAttrs = new List<Attr>();
			foreach (Attr att in attributes)
			{
				if (att.CommandType == null)
				{
					// This is clean attribute (no TAL command)
					commandAttrs.Add(att);
				}
				else
				{
					// Break up the list of attribute args
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
						Attr tmpAtt = new Attr
						{
							CommandType = att.CommandType,
							Name = stmtBits[0].Trim(' ', '\r', '\n'),
							Value = string.Join(" ", stmtBits.GetRange(1, stmtBits.Count - 1).ToArray()),
							Eq = @"=",
							Quote = @"""",
							QuoteEntity = Utils.Char2Entity(@"""")
						};
						commandAttrs.Add(tmpAtt);
					}
				}
			}
			Command ci = new Command();
			ci.Tag = currentStartTag;
			ci.CommandType = CommandType.TAL_ATTRIBUTES;
			ci.Attributes = new List<object>
			{
				commandAttrs
			};
			return ci;
		}

		protected Command Compile_TAL_OMITTAG(List<Attr> attributes)
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

			Command ci = new Command();
			ci.Tag = currentStartTag;
			ci.CommandType = CommandType.TAL_OMITTAG;
			ci.Attributes = new List<object>
			{
				expression
			};
			return ci;
		}

		// METAL compilation commands go here
		protected Command Compile_METAL_DEFINE_MACRO(List<Attr> attributes)
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
			TemplateProgramMacro macro = new TemplateProgramMacro(argument, commandList.Count, endTagSymbol);
			macroMap.Add(argument, macro);

			return null;
		}

		protected Command Compile_METAL_USE_MACRO(List<Attr> attributes)
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
			Command ci = new Command();
			ci.Tag = currentStartTag;
			ci.CommandType = CommandType.METAL_USE_MACRO;
			ci.Attributes = new List<object>
			{
				argument,
				new Dictionary<string, TemplateProgramMacro>(),
				new List<TALDefineInfo>(),
				endTagSymbol
			};
			return ci;
		}

		protected Command Compile_METAL_DEFINE_SLOT(List<Attr> attributes)
		{
			// Only last declared attribute is valid
			string argument = attributes[attributes.Count - 1].Value;

			// Compile a define-slot command, resulting argument is:
			// Argument: macroName, endTagSymbol

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

			Command ci = new Command();
			ci.Tag = currentStartTag;
			ci.CommandType = CommandType.METAL_DEFINE_SLOT;
			ci.Attributes = new List<object>
			{
				argument,
				endTagSymbol
			};
			return ci;
		}

		protected Command Compile_METAL_FILL_SLOT(List<Attr> attributes)
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
				int macroLocation = tagStack[location].UseMacroLocation;
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
			string macroName = (string)cmnd.Attributes[0];
			Dictionary<string, TemplateProgramMacro> slotMap = (Dictionary<string, TemplateProgramMacro>)cmnd.Attributes[1];
			List<TALDefineInfo> paramMap = (List<TALDefineInfo>)cmnd.Attributes[2];
			int endSymbol = (int)cmnd.Attributes[3];

			if (slotMap.ContainsKey(argument))
			{
				string msg = string.Format("Slot {0} has already been filled!", argument);
				throw new TemplateParseException(currentStartTag, msg);
			}

			// The slot starts at the next command.
			TemplateProgramMacro slot = new TemplateProgramMacro(argument, commandList.Count, endTagSymbol);
			slotMap.Add(argument, slot);

			// Update the command
			Command ci = new Command();
			ci.Tag = cmnd.Tag;
			ci.CommandType = cmnd.CommandType;
			ci.Attributes = new List<object>
			{
				macroName,
				slotMap,
				paramMap,
				endSymbol
			};
			commandList[(int)ourMacroLocation] = ci;
			return null;
		}

		protected Command Compile_METAL_DEFINE_PARAM(List<Attr> attributes)
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

			Command ci = new Command();
			ci.Tag = currentStartTag;
			ci.CommandType = CommandType.METAL_DEFINE_PARAM;
			ci.Attributes = new List<object>
			{
				commandArgs
			};
			return ci;
		}

		protected Command Compile_METAL_FILL_PARAM(List<Attr> attributes)
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
				int macroLocation = tagStack[location].UseMacroLocation;
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
			string macroName = (string)cmnd.Attributes[0];
			Dictionary<string, TemplateProgramMacro> slotMap = (Dictionary<string, TemplateProgramMacro>)cmnd.Attributes[1];
			List<TALDefineInfo> paramMap = (List<TALDefineInfo>)cmnd.Attributes[2];
			int endSymbol = (int)cmnd.Attributes[3];

			// Append param definitions to list
			paramMap.AddRange(commandArgs);

			// Update the command
			Command ci = new Command();
			ci.Tag = cmnd.Tag;
			ci.CommandType = cmnd.CommandType;
			ci.Attributes = new List<object>
			{
				macroName,
				slotMap,
				paramMap,
				endSymbol
			};
			commandList[(int)ourMacroLocation] = ci;
			return null;
		}

		protected Command Compile_METAL_IMPORT(List<Attr> attributes)
		{
			// Join attributes for commands that support multiple attributes
			string argument = string.Join(";", attributes.Select(a => a.Value).ToArray());

			// Compile a import command, resulting argument is:
			// Argument: [([importNs] importPath),...], endTagSymbol

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

		protected void SetTALPrefix(string prefix)
		{
			tal_namespace_prefix = prefix;
			tal_namespace_omittag = string.Format("{0}:omit-tag", tal_namespace_prefix);
			tal_namespace_omitscope = string.Format("{0}:omit-scope", tal_namespace_prefix);
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
