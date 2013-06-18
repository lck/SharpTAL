.. _tutorial-label:

###########
Usage Guide
###########

This text briefly introduces you to the basic usage of ``SharpTAL`` templating engine.

*************
Basic Example
*************

Here's a snippet of code that shows how easy the library is to use

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
