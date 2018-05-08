Changes
=======

3.0.1 (2017-05-05)
------------------

Internal:

- Tests were converted to NUnit version 3. [Evgeny Cherkashin]
  Removed content of packages. Now it is restored during build.
  Dependency packages versions updated to the latest.

Bugs Fixed:

- Code generator used "null" namespace, it is filtered out. [Evgeny Cherkashin]


3.0 (2014-03-04)
----------------

- Macro parameters can be declared without default values
- Removed runtime dependency on ICSharpCode.NRefactory


2.1 (2013-05-30)
----------------

- Improved type definition resolution of variables defined in globals dictionary


2.0 (2013-01-18)
----------------

Features:

- Add support for plain text templates
- Create NuGet package

Dependency Changes:

- SharpTAL now relies on ICSharpCode.NRefactory 5.3.0
- .NET 4.0 is now required


2.0b1 (2013-01-04)
------------------

Features:

- Added support for code blocks using the `<?csharp ... ?>` processing instruction syntax.
- Enable expression interpolation in CDATA [Roman Lacko]
- The "Template" class now provides virtual method "FormatResult(object)" to enable customization of expression results formatting. [Roman Lacko]

Backwards Incompatibilities:

- Removed "RenderTemplate()" methods from "ITemplateCache" interface (and it's implementations). [Roman Lacko]


2.0a2 (2012-01-05)
------------------

Features:

- New "meta:interpolation" command to control expression interpolation setting. [Roman Lacko]
  To disable expression interpolation: meta:interpolation="false"
  To enable expression interpolation: meta:interpolation="true"

Internal:

- More code refactoring. [Roman Lacko]

Bugs fixed:

- Tags in the custom tal/metal namespace were not ommited, if the custom namespace was declared on that tag. [Roman Lacko]

Backwards Incompatibilities:

- Rename "tal:define:set" variable context definition to "tal:define:nonlocal" to declare that the listed identifiers refers to previously bound variables in the nearest enclosing scope. [Roman Lacko]
- Removed "<tal:omit-scope>". It was non standart and introduces bad design in template. [Roman Lacko]


2.0a1 (2011-12-20)
------------------

Features:

- New HTML/XML template parser. This adds support for HTML5 templates. [Roman Lacko]
- String expression interpolation using ${...} operator in element attributes and in the text of an element. [Roman Lacko]
- New "Template" class that replaces the direct usage of "MemoryTemplateCache" and "FileSystemTemplateCache". [Roman Lacko]
- Allow setting CultureInfo for string formatting, default to InvariantCulture [Petteri Aimonen]
- Added method CompileTemplate() to ITemplateCache to precompile template before knowing the global variable values [Petteri Aimonen]

Internal:

- Code refactoring. [Roman Lacko]
- Add relevant lines of the generated source code to CompileSourceException message [Petteri Aimonen]
- Made template hash calculation more robus [Petteri Aimonen]

Backwards Incompatibilities:

- Removed "Inline Templates" from ITemplateChache.RenderTemplate method. Use "metal:import" command to import macros from external templates [Roman Lacko]

Dependency Changes:

- SharpTAL now relies on ICSharpCode.NRefactory.dll [Roman Lacko]

Bugs fixed:

- In SourceGenerator, fix the handling of newlines in attributes [Petteri Aimonen]


1.2 (2011-01-26)
----------------

- Fixed tal:repeat command when using with empty arrays [Roman Lacko]


1.1 (2010-10-25)
----------------

- Unit Tests ported to NUnit [Roman Lacko]
- Mono 2.6 with MonoDevelop 2.4 now supported under Linux (tested under Ubuntu 10.10) [Roman Lacko]
- .NET Framework 3.5 and 4.0 with Sharpdevelop 4.0beta3 supported under Windows [Roman Lacko]


1.0 (2010-06-28)
----------------

- Initial version [Roman Lacko]
