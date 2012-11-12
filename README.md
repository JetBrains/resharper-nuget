# resharper-nuget

This plugin for ReSharper adds support for NuGet references to ReSharper.

Once installed, when ReSharper tries to import a type from an assembly referenced by another project, and that assembly is a NuGet package, then ReSharper will use NuGet to add the reference, correctly updating packages.config, running any .ps1 scripts and installing any required dependencies.

From the end user's perspective, there is no noticeable user interface. When you use a type that lives in an assembly that is not part of the assemblies currently referenced, but is in an assembly referenced by other projects in the solution, ReSharper will mark the type as an error, and display the context action icon when the cursor is on the type name (the red light bulb). Pressing Alt-Enter displays the action "reference 'asm' and use 'Asm.Type'". This plugin hooks into that process. Selecting that action for an assembly referenced as part of a NuGet package will be installed by NuGet.

## Building ##

To build the source, you need the [ReSharper 7 SDK](http://www.jetbrains.com/resharper/download/index.html) installed (ReSharper 6.1.1 support requires the [ReSharper 6.1 SDK, which can be downloaded from the previous versions archive](http://devnet.jetbrains.net/docs/DOC-280)). Then just open the resharper-nuget.sln file and build.

## Installing

If you wish to just install a copy of the plugins without building yourself:

- Visit the [downloads section on GitHub](https://github.com/JetBrains/resharper-nuget/downloads)
- Download the latest zip file
- Extract everything
- Run the appropriate batch file for your version of ReSharper, e.g. Install-NuGetSupport.7.0.bat for ReSharper 7.0

## Version

The current version is 0.2, and should be treated as a beta version. It has been tested with Visual Studio 2012 and 2010, and supports ReShaprer 6.1.1, 7.0 and 7.1 (EAP build 22)

## Contributing ##

Feel free to raise issues on GitHub, or [fork the project](http://help.github.com/fork-a-repo/) and [send a pull request](http://help.github.com/send-pull-requests/).

## Roadmap

Please see the Issues tab on JetBrains' GitHub page.

