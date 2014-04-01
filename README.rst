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

The basic TAL (Template Attribute Language) example:

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
By default, the string is escaped before insertion. To avoid this, use the *structure:* prefix:

.. code-block:: html

    <div>${structure: ...}</div>

The macro language (known as the macro expansion language or METAL) provides a means of filling in portions of a generic template.

The macro template (saved as main.html file):

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

Template that imports and uses the macro, filling in the ``content`` slot:

.. code-block:: html

    <metal:tag metal:import="main.html" use-macro='macros["main"]'>
      <p metal:fill-slot="content">${structure: document.body}<p/>
    </metal:tag>

In the example, the statement *metal:import* is used to import a template from the file system using a path relative to the calling template.

Sample code that shows how easy the library is to use:

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
