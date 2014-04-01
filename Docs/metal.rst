.. _metal_chapter:

Macros (METAL)
==============

The *Macro Expansion Template Attribute Language* (METAL) standard is
a facility for HTML/XML macro preprocessing. It can be used in
conjunction with or independently of TAL and TALES.

Macros provide a way to define a chunk of presentation in one
template, and share it in others, so that changes to the macro are
immediately reflected in all of the places that share it.
Additionally, macros are always fully expanded, even in a template's
source text, so that the template appears very similar to its final
rendering.

A single Page Template can accomodate multiple macros.

Namespace
---------

The METAL namespace URI and recommended alias are currently defined
as::

        xmlns:metal="http://xml.zope.org/namespaces/metal"

Just like the TAL namespace URI, this URI is not attached to a web
page; it's just a unique identifier.  This identifier must be used in
all templates which use METAL.

Statements
----------

METAL defines a number of statements:

* ``metal:define-macro`` Define a macro.
* ``metal:use-macro`` Use a macro.
* ``metal:define-slot`` Define a macro customization point.
* ``metal:fill-slot`` Customize a macro.
* ``metal:define-param`` Define macro parameters.
* ``metal:fill-param`` Fill macro parameters.
* ``metal:import`` Import macro definitions from external file.

Although METAL does not define the syntax of expression non-terminals,
leaving that up to the implementation, a canonical expression syntax
for use in METAL arguments is described in TALES Specification.

metal:define-macro
------------------

Define a macro

Syntax
~~~~~~

``metal:define-macro`` syntax::

    argument ::= Name

Description
~~~~~~~~~~~

The ``metal:define-macro`` statement defines a macro. The macro is named
by the statement expression, and is defined as the element and its
sub-tree.

Examples
~~~~~~~~

Simple macro definition:

.. code-block:: html

    <p metal:define-macro="copyright">
      Copyright 2004, <em>Foobar</em> Inc.
    </p>

metal:define-slot
-----------------

Define a macro customization point

Syntax
~~~~~~

``metal:define-slot`` syntax::

    argument ::= Name

Description
~~~~~~~~~~~

The ``metal:define-slot`` statement defines a macro customization
point or *slot*. When a macro is used, its slots can be replaced, in
order to customize the macro. Slot definitions provide default content
for the slot. You will get the default slot contents if you decide not
to customize the macro when using it.

The ``metal:define-slot`` statement must be used inside a
``metal:define-macro`` statement.

Slot names must be unique within a macro.

Examples
~~~~~~~~

Simple macro with slot:

.. code-block:: html

    <p metal:define-macro="hello">
      Hello <b metal:define-slot="name">World</b>
    </p>

This example defines a macro with one slot named ``name``. When you use
this macro you can customize the ``b`` element by filling the ``name``
slot.

metal:fill-slot
---------------

Customize a macro

Syntax
~~~~~~

``metal:fill-slot`` syntax::

        argument ::= Name

Description
~~~~~~~~~~~

The ``metal:fill-slot`` statement customizes a macro by replacing a
*slot* in the macro with the statement element (and its content).

The ``metal:fill-slot`` statement must be used inside a
``metal:use-macro`` statement.

Slot names must be unique within a macro.

If the named slot does not exist within the macro, the slot
contents will be silently dropped.

Examples
~~~~~~~~

Given this macro:

.. code-block:: html

    <p metal:define-macro="hello">
      Hello <b metal:define-slot="name">World</b>
    </p>

You can fill the ``name`` slot like so:

.. code-block:: html

    <p metal:use-macro='master.macros["hello"]'>
      Hello <b metal:fill-slot="name">Kevin Bacon</b>
    </p>

metal:use-macro
---------------

Use a macro

Syntax
~~~~~~

``metal:use-macro`` syntax::

    argument ::= expression

Description
~~~~~~~~~~~

The ``metal:use-macro`` statement replaces the statement element with
a macro. The statement expression describes a macro definition.

The effect of expanding a macro is to graft a subtree from another
document (or from elsewhere in the current document) in place of the
statement element, replacing the existing sub-tree.  Parts of the
original subtree may remain, grafted onto the new subtree, if the
macro has *slots*. See ``metal:define-slot`` for more information. If
the macro body uses any macros, they are expanded first.

Examples
~~~~~~~~

Basic macro usage:

.. code-block:: html

    <p metal:use-macro='other.macros["header"]'>
      header macro from defined in other.html template
    </p>

This example refers to the ``header`` macro defined in the ``other``
template which has been passed as a keyword argument to ``ITemplateCache``'s
``RenderTemplate`` method. When the macro is expanded, the ``p`` element and
its contents will be replaced by the macro.

metal:define-param
------------------

Define a macro parameter

Syntax
~~~~~~

``metal:define-param`` syntax::

    argument             ::= attribute_statement [';' attribute_statement]*
    attribute_statement  ::= param_type param_name [expression]
    param_type           ::= Parameter Type
    param_name           ::= Parameter Name

Description
~~~~~~~~~~~

The ``metal:define-param`` statement defines macro parameters.
When you define a parameter it can be used as a normal local variable
in a macro element, and the elements it contains.

The ``metal:define-param`` statement must be used inside a
``metal:define-macro`` statement.

Parameter names must be unique within a macro.

Examples
~~~~~~~~

Simple macro with two parameters:

.. code-block:: html

    <p metal:define-macro="hello"
       metal:define-param='string name; int age'>
      Hello, my name is <b>${name}</b>.
      I'm <b>${age}</b> years old.
    </p>

You can declare parameters with default values:

.. code-block:: html

    <p metal:define-macro="hello"
       metal:define-param='string name "Roman"; int age 33'>
      Hello, my name is <b>${name}</b>.
      I'm <b>${age}</b> years old.
    </p>

metal:fill-param
----------------

Fill a macro parameter

Syntax
~~~~~~

``metal:fill-param`` syntax::

    argument             ::= attribute_statement [';' attribute_statement]*
    attribute_statement  ::= param_name expression
    param_name           ::= Parameter Name

Description
~~~~~~~~~~~

The ``metal:fill-param`` statement fills macro parameters.

The ``metal:fill-param`` statement must be used inside a ``metal:use-macro`` statement.

If the named parameter does not exist within the macro, the parameter
contents will be silently dropped.

Examples
~~~~~~~~

Given this macro:

.. code-block:: html

    <p metal:define-macro="hello"
       metal:define-param='string name; int age'>
      Hello, my name is <b>${name}</b>.
      I'm <b>${age}</b> years old.
    </p>

You can fill the ``name`` and ``age`` parameters like so:

.. code-block:: html

    <p metal:use-macro='master.macros["hello"]'
       metal:fill-param='name "Roman"; age 33'>
    </p>

metal:import
------------

Import macro definitions from external file

Syntax
~~~~~~

``metal:import`` syntax::

    argument             ::= import_statement [';' import_statement]*
    import_statement     ::= (namespace_name ':') path
    namespace_name       ::= Namespace name
    path                 ::= Path to file

Description
~~~~~~~~~~~

The ``metal:import`` statement imports macro defintions from external files.
Macros can be imported to specific namespace, defined by ``namespace`` argument part.
If the namespace is not specified, macros are imported to default namespace.

Examples
~~~~~~~~

Import macros from file ``Macros.html`` into default namespace and use imported macro ``hello``:

.. code-block:: html

    <p metal:import="Macros.html">
      <p metal:use-macro='macros["hello"]'
         metal:fill-param='name "Roman"; age 33'>
      </p>
    </p>

Import macros from file ``Macros.html`` into custom namespace ``mymacros`` and use imported macro ``hello``:

.. code-block:: html

    <p metal:import="mymacros:Macros.html">
      <p metal:use-macro='mymacros.macros["hello"]'
         metal:fill-param='name "Roman"; age 33'>
      </p>
    </p>

Import macros from multiple files into one custom namespace:

.. code-block:: html

    <p metal:import="mymacros:Macros1.html;mymacros:Macros2.html">
    </p>

Import macros from multiple files into multiple custom namespaces:

.. code-block:: html

    <p metal:import="mymacros1:Macros1.html;mymacros2:Macros2.html">
    </p>
