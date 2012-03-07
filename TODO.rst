====
TODO
====

- Implement statement "meta:reference" to add assembly references:
    <div meta:reference="System.Data.dll" />
	multiple references:
    <div meta:reference="System.Data.dll; System.Xml.dll" />

- CodeGenerator: implement metal:macros as true class methods

- Make the Template class to be "wrapper" around Render() methods generated from xml/html template.

- Make "default" expression (local) variable, so it can be used in expressions like "default + xyz" (This makes sence only in attributes?)

- Implement support for ASP.NET MVC View Engine

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

- Implement statements:
	metal:extend-macro
	tal:switch / tal:case
    i18n:translate
    i18n:domain
    i18n:source
    i18n:target
    i18n:name
    i18n:attributes
    i18n:data

- Compile dynamic assemblies in separate app domain:
    http://code.google.com/p/jplabscode/source/browse/trunk/?r=107#trunk%2FDynamicCode%2FDynamicCode%253Fstate%253Dclosed
