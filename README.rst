SharpTAL
========

SharpTAL is an HTML/XML template engine for .NET platform,
that you can use in any application running on .NET 4.0.

The template engine compiles HTML/XML templates into .NET assemblies.

It contains implementation of the ZPT language (Zope Page Templates).
ZPT is a system which can generate HTML, XML or plain text output.
ZPT is formed by the `TAL (Template Attribute Language) <https://sharptal.readthedocs.org/en/latest/tal.html>`_,
`TALES (TAL Expression Syntax) <https://sharptal.readthedocs.org/en/latest/tales.html>`_
and the `METAL (Macro Expansion TAL) <https://sharptal.readthedocs.org/en/latest/metal.html>`_.

Getting the code
----------------

Binaries are provided as a NuGet package (`https://nuget.org/packages/SharpTAL <https://nuget.org/packages/SharpTAL/>`_).

The project is hosted in a GitHub `repository <http://github.com/lck/SharpTAL/>`_

Please report any issues to the `issue tracker <http://github.com/lck/SharpTAL/issues>`_.

Introduction
------------

Using a set of simple language constructs, you control the document flow, element repetition and text replacement.

The basic TAL (Template Attribute Language) example

.. code-block:: html

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

The macro template (saved as main.html file)

.. code-block:: html

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

Template that imports and uses the macro, filling in the “content” slot

.. code-block:: html

    <metal:tag metal:import="main.html" use-macro='macros["main"]'>
      <p metal:fill-slot="content">${structure: document.body}<p/>
    </metal:tag>

In the example, the statement *metal:import* is used to import a template from the file system using a path relative to the calling template.

Here’s a sample code that shows how easy the library is to use:

.. code-block:: csharp

    var globals = new Dictionary<string, object>
    {
        { "movies", new List<string> { "alien", "star wars", "star trek" } }
    };

    const string body = @"<!DOCTYPE html>
    <html tal:define='textInfo new System.Globalization.CultureInfo(""en-US"", false).TextInfo'>
        Favorite sci-fi movies:
        <div tal:repeat='movie movies'>${textInfo.ToTitleCase(movie)}</div>
    </html>";

    var template = new Template(body);

    var result = template.Render(globals);

    Console.WriteLine(result);

Here's the console output:

.. code-block:: html

    <!DOCTYPE html>
    <html>
       Favorite sci-fi movies:
       <div>Alien</div><div>Star Wars</div><div>Star Trek</div>
    </html>


License
-------

This software is made available under `Apache Licence Version 2.0 <http://www.apache.org/licenses/LICENSE-2.0>`_.

Planned features
----------------

- Integration with .NET MVC as ViewEngine
- IronPython support in template expressions
- i18 support

Changes
-------

2.1 (2013-05-30)
~~~~~~~~~~~~~~~~

Features:

- Significantly improved the type definition resolution of variables defined in globals dictionary


2.0 (2013-01-18)
~~~~~~~~~~~~~~~~

Features:

- Add support for plain text templates
- Create NuGet package

Dependency Changes:

- SharpTAL now relies on ICSharpCode.NRefactory 5.3.0
- .NET 4.0 is now required


2.0b1 (2013-01-04)
~~~~~~~~~~~~~~~~~~

Features:

- Added support for code blocks using the `<?csharp ... ?>` processing instruction syntax.
- Enable expression interpolation in CDATA [Roman Lacko]
- The "Template" class now provides virtual method "FormatResult(object)" to enable customization of expression results formatting. [Roman Lacko]

Internal:

Backwards Incompatibilities:

- Removed "RenderTemplate()" methods from "ITemplateCache" interface (and it's implementations). [Roman Lacko]

Bugs fixed:

2.0a2 (2012-01-05)
~~~~~~~~~~~~~~~~~~

Features:

- New "meta:interpolation" command to control expression interpolation setting. [Roman Lacko]
  To disable expression interpolation: meta:interpolation="false"
  To enable expression interpolation: meta:interpolation="true"

Internal:

- More code refactoring. [Roman Lacko]

Backwards Incompatibilities:

- Rename "tal:define:set" variable context definition to "tal:define:nonlocal" to declare that the listed identifiers refers to previously bound variables in the nearest enclosing scope. [Roman Lacko]
- Removed "<tal:omit-scope>". It was non standart and introduces bad design in template. [Roman Lacko]

Bugs fixed:

- Tags in the custom tal/metal namespace were not ommited, if the custom namespace was declared on that tag. [Roman Lacko]

2.0a1 (2011-12-20)
~~~~~~~~~~~~~~~~~~

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
