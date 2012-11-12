# resharper-nuget

This plugin for ReSharper adds support for NuGet references to ReSharper.

Once installed, when ReSharper tries to import a type from an assembly referenced by another project, and that assembly is a NuGet package, then ReSharper will use NuGet to add the reference, correctly updating packages.config, running any .ps1 scripts and installing any required dependencies.

From the end user's perspective, there is no noticeable user interface. When you use a type that lives in an assembly that is not part of the assemblies currently referenced, but is in an assembly referenced by other projects in the solution, ReSharper will mark the type as an error, and display the context action icon when the cursor is on the type name (the red light bulb). Pressing Alt-Enter displays the action "reference 'asm' and use 'Asm.Type'". This plugin hooks into that process. Selecting that action for an assembly referenced as part of a NuGet package will be installed by NuGet.

For example, if project Test1 adds a NuGet reference to [the xUnit.net: extensions package](http://nuget.org/packages/xunit.extensions/1.9.1),
NuGet will also download [the xUnit.net package](http://nuget.org/packages/xunit/1.9.1), and extracts the files to the
packages\xunit.1.9.1\lib\net20 and packages\xunit.extensions.1.9.1\lib\net20 folders. NuGet will then add a reference to
xunit.extensions.dll and xunit.dll, and run any script files that are in the packages.

If you then add a new project to the solution, Test2, and before adding any references, try to use TheoryAttribute, ReSharper
will mark that usage as an error. It also looks for any types called TheoryAttribte in any assembly that's known in the
solution. It finds Xunit.Extensions.TheoryAttribute from xunit.extensions.dll being referenced by Test1, and offers a
quick-fix red lightbulb for the usage of TheoryAttribute. When you hit alt-enter, you'll see a menu option that reads
"reference 'xunit.extensions' and use 'Xunit.Extensions.TheoryAttribute'".

Without the plugin, ReSharper would now just add a reference to packages\xunit.extensions.1.9.1\lib\net20\xunit.extensions.dll file.
With the plugin, ReSharper knows that this is a NuGet pacakge, and invokes NuGet to install xunit.extensions in the Test2
project. NuGet adds the references, updates the packages.config file, and also installs the dependency on the xunit package.


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

