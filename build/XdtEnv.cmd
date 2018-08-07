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