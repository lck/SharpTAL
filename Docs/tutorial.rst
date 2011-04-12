.. _tutorial-label:

###########
Usage Guide
###########

This text briefly introduces you to the basic usage of ``SharpTAL`` templating engine.

*************
Basic Example
*************

Here's a snippet of code that shows how easy the library is to use::

    using System;
    using System.IO;
    using System.Reflection;
    using System.Collections.Generic;

    using SharpTAL;

    namespace Demo
    {
        class Demo
        {
            static void Main(string[] args)
            {
                // Set path to the existing cache folder
                string cacheFolder = Path.Combine(Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location), "Template Cache");

                // Create the template cache.
                // We want to clear the cache folder on startup, setting the clearCache parameter to true,
                // and using customized file name pattern.
                ITemplateCache cache = new FileSystemTemplateCache(cacheFolder, true, @"Demo_{key}.dll");

                // The body of the template
                string templateBody = @"<html><h1 tal:content=""title"">The title goes here</h1></html>";

                // Global variables used in template
                Dictionary<string, object> globals = new Dictionary<string, object>();
                globals["title"] = "Hello World !";

                // Finally, render the template. In this moment the assembly will be generated and cached
                string slowResult = cache.RenderTemplate(templateBody, globals);

                // The "slowResult" will contain: <html><h1>Hello World !</h1></html>

                // Set the title to another value
                globals["title"] = "Hi !";

                // A second call to RenderRemplate() will use cached assembly
                string fastResult = cache.RenderTemplate(templateBody, globals);

                // The "fastResult" will contain: <html><h1>Hi !</h1></html>
            }
        }
    }

*****************
Structure Example
*****************

TODO:

*************
METAL Example
*************

TODO:
