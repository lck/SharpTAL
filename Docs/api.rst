.. _api-label:

#############
API Reference
#############

===================
MemoryTemplateCache
===================

.. class:: SharpTAL.FileSystemTemplateCache()
   
   Represents the in-memory template cache
   
   .. function:: RenderTemplate(StreamWriter output, string templateBody, Dictionary<string, object> globals, Dictionary<string, string> inlineTemplates, List<Assembly> referencedAssemblies, out TemplateInfo templateInfo)
      
      Renders the xml template to output stream
      
      :param output: The output stream
      :param templateBody: Template content. It must be valid xml
      :param globals: Dictionary of global variables
      :param inlineTemplates: Dictionary of inline templates (macros)
      :param referencedAssemblies: List of referenced assemblies
      :param templateInfo: Info about the generated template
   
   .. function:: RenderTemplate(string templateBody, Dictionary<string, object> globals, Dictionary<string, string> inlineTemplates, List<Assembly> referencedAssemblies, out TemplateInfo templateInfo)
      
      Renders the xml template and returns the result as string
      
      :param templateBody: Template content. It must be valid xml
      :param globals: Dictionary of global variables
      :param inlineTemplates: Dictionary of inline templates (macros)
      :param referencedAssemblies: List of referenced assemblies
      :param templateInfo: Info about the generated template

=======================
FileSystemTemplateCache
=======================

.. class:: SharpTAL.FileSystemTemplateCache(string cacheFolder[, bool clearCache[, string pattern]])
   
   Represents the filesystem template cache
   
   :param cacheFolder: Full path to folder where assemblies will be generated and searched
   :param clearCache: If set to true, all files with name matching pattern will be deleted (Default is false)
   :param pattern: File name pattern of generated assemblies (Default is @"Template_{key}.dll")
   
   .. function:: RenderTemplate(StreamWriter output, string templateBody, Dictionary<string, object> globals, Dictionary<string, string> inlineTemplates, List<Assembly> referencedAssemblies, out TemplateInfo templateInfo)
      
      Renders the xml template to output stream
      
      :param output: The output stream
      :param templateBody: Template content. It must be valid xml
      :param globals: Dictionary of global variables
      :param inlineTemplates: Dictionary of inline templates (macros)
      :param referencedAssemblies: List of referenced assemblies
      :param templateInfo: Info about the generated template
   
   .. function:: RenderTemplate(string templateBody, Dictionary<string, object> globals, Dictionary<string, string> inlineTemplates, List<Assembly> referencedAssemblies, out TemplateInfo templateInfo)
      
      Renders the xml template and returns the result as string
      
      :param templateBody: Template content. It must be valid xml
      :param globals: Dictionary of global variables
      :param inlineTemplates: Dictionary of inline templates (macros)
      :param referencedAssemblies: List of referenced assemblies
      :param templateInfo: Info about the generated template
