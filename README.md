# resharper-nuget

This plugin for ReSharper adds support for NuGet references to ReSharper.

Once installed, when ReSharper tries to import a type from an assembly referenced by another project, and that assembly is a NuGet package, then ReSharper will use NuGet to add the reference, correctly updating packages.config, running any .ps1 scripts and installing any required dependencies.

From the end user's perspective, there is no noticeable user interface. When you use a type that lives in an assembly that is not part of the assemblies currently referenced, but is in an assembly referenced by other projects in the solution, ReSharper will mark the type as an error, and display the context action icon when the cursor is on the type name (the red light bulb). Pressing Alt-Enter displays the action "reference 'asm' and use 'Asm.Type'". This plugin hooks into that process. Selecting that action for an assembly referenced as part of a NuGet package will be installed by NuGet.

## Installing

To install, you can download a pre-built version in the downloads section. Extract the zip file and run the appropriate batch file for your version of ReSharper.

## Version

The current version is 0.2, and should be treated as a beta version. It has been tested with Visual Studio 2012 and 2010, and supports ReShaprer 6.1.1, 7.0 and 7.1 (EAP build 22)

## Roadmap

Please see the Issues tab on JetBrains' GitHub page.

