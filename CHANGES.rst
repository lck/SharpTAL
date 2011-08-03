=======
Changes
=======

1.3 (not released)
==================

Features added
--------------

- New HTML/XML template parser (ported from project Chameleon: https://github.com/malthe/chameleon) [Roman Lacko]

Other changes
-------------

- Added method PrecompileTemplate() to AbstractTemplateCache to precompile template before knowing the global variable values [Petteri Aimonen]
- Allow setting CultureInfo for string formatting, default to InvariantCulture [Petteri Aimonen]
- Add relevant lines of the generated source code to CompileSourceException message [Petteri Aimonen]
- Made template hash calculation more robus [Petteri Aimonen]

Bugs fixed
----------

- In SourceGenerator, fix the handling of newlines in attributes [Petteri Aimonen]


1.2 (released 26.01.2011)
=========================

- Fixed tal:repeat command when using with empty arrays


1.1 (released 25.10.2010)
=========================

- Unit Tests ported to NUnit
- Mono 2.6 with MonoDevelop 2.4 now supported under Linux (tested under Ubuntu 10.10)
- .NET Framework 3.5 and 4.0 with Sharpdevelop 4.0beta3 supported under Windows


1.0 (released 28.06.2010)
=========================

- Initial version
