@echo off

if not defined XdtRoot (
    echo Initializing Xdt environment
    call "%~dp0\XdtEnv.cmd"
)
