@echo off
setlocal enableextensions

mkdir NuGetSupport.6.1 2> NUL
copy /y ..\src\resharper-nuget\bin\Release\*.6.1.* NuGetSupport.6.1\

mkdir NuGetSupport.7.1 2> NUL
copy /y ..\src\resharper-nuget\bin\Release\*.7.1.* NuGetSupport.7.1\

mkdir NuGetSupport.8.0 2> NUL
copy /y ..\src\resharper-nuget\bin\Release\*.8.0.* NuGetSupport.8.0\
