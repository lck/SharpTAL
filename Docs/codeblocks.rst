.. _codeblocks_chapter:

Code blocks
===========

The ``<?csharp ... ?>`` notation allows you to embed C# code in
templates:

.. code-block:: html

    <div>
      <?csharp
        var numbers = Enumerable.Range(1, 10).Select(n => n.ToString()).ToList()
      ?>
      Please input a number from the range ${string.Join(", ", numbers)}.
    </div>
