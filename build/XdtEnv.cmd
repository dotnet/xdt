@echo off

if not defined BuildConfiguration (
    set "BuildConfiguration=Release"
)

if not defined FeedTasksPackage (
    set FeedTasksPackage=Microsoft.DotNet.Build.Tasks.Feed
)

if not defined FeedTasksPackageVersion (
    set FeedTasksPackageVersion=2.1.0-prerelease-02419-02
)

set "XdtRoot=%~dp0"
set "XdtRoot=%XdtRoot:~0,-7%"
set "PATH=%PATH%;%XdtRoot%"


if defined ProgramFiles(x86) (
    set "XdtProgramFiles=%ProgramFiles(x86)%"
) else (
    set "XdtProgramFiles=%ProgramFiles%"
)

if exist "%XdtProgramFiles%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe" (
    set "XdtMSBuildPath=%XdtProgramFiles%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin"
) else (
    set "XdtMSBuildPath=%XdtProgramFiles%\MSBuild\14.0\Bin"
)

set "PATH=%PATH%;%XdtMSBuildPath%"

set "PATH=%PATH%;%ProgramFiles%\dotnet\;"