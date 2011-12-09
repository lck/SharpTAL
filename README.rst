SharpTAL
========

SharpTAL is an HTML/XML template engine for .NET platform,
with minimal dependencies that you can use in any application
running on .NET Framework ver. 3.5 and up or Mono ver. 2.6.

The template engine compiles HTML/XML templates into .NET assemblies.

It contains implementation of the ZPT language (Zope Page Templates).
ZPT is a system which can generate HTML, XML or plain text output.
ZPT is formed by the TAL (Template Attribute Language), TALES (TAL Expression Syntax) and the METAL (Macro Expansion TAL).

First versions of SharpTAL were based on `SimpleTAL <http://www.owlfish.com/software/simpleTAL/>`_,
the python implementation of page templates.

Latest version of SharpTAL implements the template parser ported from another excellent template engine,
the `Chameleon project <http://github.com/malthe/chameleon/>`_.

Getting the code
----------------

You can download the binary packages from the codeplex `home page <http://sharptal.codeplex.com/>`_.
Latest version relies on ICSharpCode.NRefactory.dll library (included in latest binary package).

The project is hosted in a GitHub `repository <http://github.com/lck/SharpTAL/>`_

Please report any issues to the `issue tracker <http://github.com/lck/SharpTAL/issues>`_.

Introduction
------------

Using a set of simple language constructs, you control the document flow, element repetition and text replacement.

The basic TAL (Template Attribute Language) example::

    <html>
      <body>
        <h1>Hello, ${"world"}!</h1>
        <table>
          <tr tal:repeat='row new string[] { "red", "green", "blue" }'>
            <td tal:repeat='col new string[] { "rectangle", "triangle", "circle" }'>
               ${row} ${col}
            </td>
          </tr>
        </table>
      </body>
    </html>

The ${...} notation is short-hand for text insertion. The C# expression inside the braces is evaluated and the result included in the output.
By default, the string is escaped before insertion. To avoid this, use the *structure:* prefix::

    <div>${structure: ...}</div>

The macro language (known as the macro expansion language or METAL) provides a means of filling in portions of a generic template.

The macro template (saved as main.html file)::

    <html metal:define-macro="main">
      <head>
        <title>Example ${document.title}</title>
      </head>
      <body>
        <h1>${document.title}</h1>
        <div id="content">
          <metal:tag metal:define-slot="content" />
        </div>
      </body>
    </html>

Template that imports and uses the macro, filling in the “content” slot::

    <metal:tag metal:import="main.html" use-macro='macros["main"]'>
      <p metal:fill-slot="content">${structure: document.body}<p/>
    </metal:tag>

In the example, the statement *metal:import* is used to import a template from the file system using a path relative to the calling template.

Here’s a complete sample code that shows how easy the library is to use::

    using System;
    using System.Collections.Generic;

    using SharpTAL;

    namespace Demo
    {
        public class User
        {
            public string Name { get; set; }
        }

        class Sample
        {
            static void Main(string[] args)
            {
                // Objects used in template
                User user = new User { Name = "Roman" };

                // The template with template body
                Template template = new Template(@"<html><h1>Hello ${user.Name}!</h1></html>");

                // Dictionary of globals variables used in template
                Dictionary<string, object> globals = new Dictionary<string, object> { { "user", user } };
                
                // Render the template. In this moment the assembly will be generated and cached
                // Result: <html><h1>Hello Roman!</h1></html>
                Console.WriteLine(template.Render(globals));

                // Set the user name to another value
                user.Name = "Peter";

                // A second call to Render() will use cached assembly
                // Result: <html><h1>Hello Peter!</h1></html>
                Console.WriteLine(template.Render(globals));

                Console.ReadKey();
            }
        }
    }

License
-------

This software is made available under `Apache Licence Version 2.0 <http://www.apache.org/licenses/LICENSE-2.0>`_.

Changes
-------

2.0a1 (next release)
~~~~~~~~~~~~~~~~~~~~

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
~~~~~~~~~~~~~~~~

- Fixed tal:repeat command when using with empty arrays [Roman Lacko]

1.1 (2010-10-25)
~~~~~~~~~~~~~~~~

- Unit Tests ported to NUnit [Roman Lacko]
- Mono 2.6 with MonoDevelop 2.4 now supported under Linux (tested under Ubuntu 10.10) [Roman Lacko]
- .NET Framework 3.5 and 4.0 with Sharpdevelop 4.0beta3 supported under Windows [Roman Lacko]

1.0 (2010-06-28)
~~~~~~~~~~~~~~~~

- Initial version [Roman Lacko]
