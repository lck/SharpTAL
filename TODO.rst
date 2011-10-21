====
TODO
====

- Make "default" expression (local) variable, so it can be used in expressions like "default + xyz".
    This makes sence only in attributes

- Make string expression interpolation using ${...} operator configurable

- Compile dynamic assemblies in separate app domain:
    http://code.google.com/p/jplabscode/source/browse/trunk/?r=107#trunk%2FDynamicCode%2FDynamicCode%253Fstate%253Dclosed

- Extend metal:import command (using TALES syntax) so it would be possible to specify type of the template loader.
	Default loader would be the "file" loader:
	<div metal:import="file:c:\templates\macros.xml" />
	Is quivalent to:
	<div metal:import="c:\templates\macros.xml" />
	Load from variables:
	<div metal:import="var:macrosXmlDoc" />

- Ability to use global variable of type MacroProgram to use it as macro, for example:
	var testMacro = MacroCompiler.CompileTemplate("<div metal:define-macro='hello'>Hello World!<div/>")
	var template = new Template(
		@"<div metal:use-macro='test.macros[""hello""]'>",
		new Dictionary<string, object> { "test", testMacro });

- Implement metal:extend-macro statement

- Implement statements tal:switch and tal:case

- Use Mono Compiler Services for assembly generation

- Visual Studio plugin

- Implement support for ASP.NET MVC View Engine
