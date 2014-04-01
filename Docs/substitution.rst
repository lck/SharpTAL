.. _substitution_chapter:

${...} operator
===============

The ``${...}`` notation is short-hand for text insertion. The
C# expression inside the braces is evaluated and the result
included in the output (all inserted text is escaped by default):

.. code-block:: html

    <div id="section-${index + 1}">
      ${content}
    </div>

To escape this behavior, prefix the notation with a backslash
character: ``\${...}``.
