call "%~dp0\build\EnsureXdtEnv.cmd"

msbuild "%XdtRoot%\build\build.proj" /p:Configuration=%BuildConfiguration% /t:Build %*