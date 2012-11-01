@echo off
setlocal enableextensions

mkdir NuGetSupport.7.0 2> NUL
copy /y ..\src\resharper-nuget\bin\Release\*.7.0.* NuGetSupport.7.0\

mkdir NuGetSupport.7.1 2> NUL
copy /y ..\src\resharper-nuget\bin\Release\*.7.1.* NuGetSupport.7.1\
