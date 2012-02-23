=======
Changes
=======

2.0a3 (2012-??-??)
==================

Features
--------

- The "Template" class now provides virtual method "FormatResult(object)" to enable customization of expression results formatting.

Bugs fixed
----------

Internal
--------

Backwards Incompatibilities
---------------------------

- Removed "RenderTemplate()" methods from "ITemplateCache" interface (and it's implementations).


2.0a2 (2012-01-05)
==================

Features
--------

- New "meta:interpolation" command to control expression interpolation setting. [Roman Lacko]
  To disable expression interpolation: meta:interpolation="false"
  To enable expression interpolation: meta:interpolation="true"

Internal
--------

- More code refactoring. [Roman Lacko]

Bugs fixed
----------

- Tags in the custom tal/metal namespace were not ommited, if the custom namespace was declared on that tag. [Roman Lacko]

Backwards Incompatibilities
---------------------------

- Rename "tal:define:set" variable context definition to "tal:define:nonlocal" to declare that the listed identifiers refers to previously bound variables in the nearest enclosing scope. [Roman Lacko]
- Removed "<tal:omit-scope>". It was non standart and introduces bad design in template. [Roman Lacko]


2.0a1 (2011-12-20)
==================

Features
--------

- New HTML/XML template parser. This adds support for HTML5 templates. [Roman Lacko]
- String expression interpolation using ${...} operator in element attributes and in the text of an element. [Roman Lacko]
- New "Template" class that replaces the direct usage of "MemoryTemplateCache" and "FileSystemTemplateCache". [Roman Lacko]
- Allow setting CultureInfo for string formatting, default to InvariantCulture [Petteri Aimonen]
- Added method CompileTemplate() to ITemplateCache to precompile template before knowing the global variable values [Petteri Aimonen]

Internal
--------

- Code refactoring. [Roman Lacko]
- Add relevant lines of the generated source code to CompileSourceException message [Petteri Aimonen]
- Made template hash calculation more robus [Petteri Aimonen]

Backwards Incompatibilities
---------------------------

- Removed "Inline Templates" from ITemplateChache.RenderTemplate method. Use "metal:import" command to import macros from external templates [Roman Lacko]

Dependency Changes
------------------

- SharpTAL now relies on ICSharpCode.NRefactory.dll [Roman Lacko]

Bugs fixed
----------

- In SourceGenerator, fix the handling of newlines in attributes [Petteri Aimonen]


1.2 (2011-01-26)
================

- Fixed tal:repeat command when using with empty arrays [Roman Lacko]


1.1 (2010-10-25)
================

- Unit Tests ported to NUnit [Roman Lacko]
- Mono 2.6 with MonoDevelop 2.4 now supported under Linux (tested under Ubuntu 10.10) [Roman Lacko]
- .NET Framework 3.5 and 4.0 with Sharpdevelop 4.0beta3 supported under Windows [Roman Lacko]


1.0 (2010-06-28)
================

- Initial version [Roman Lacko]
