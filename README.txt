########
Overview
########

The ``SharpTAL`` project is implementation of Zope Page Templates (ZPT) language in .NET.

===============
Getting Started
===============

Have a look at the :ref:`Usage Guide <tutorial-label>` for a brief introduction

===================
Installing SharpTAL
===================

Download assembly and/or source code from http://sharptal.codeplex.com/

=============
Compatibility
=============

Works on the .NET Framework ver. 3.5 and 4.0 and Mono ver. 2.6.

===================
Template Generation
===================

Rendering of template is executed in following steps:

1. The XML template is parsed and C# source is generated to temporary file
2. Assembly is compiled from C# source and saved to cache folder with name [Template_<hash>.dll]
3. The static method with full name [Templates.Template_<hash>.Render()] is loaded from generated assembly and executed
