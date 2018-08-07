call "%~dp0\build\EnsureXdtEnv.cmd"

msbuild "%XdtRoot%\build\publish\PublishPackages.csproj" /t:Restore
msbuild "%XdtRoot%\build\publish\publish.proj" /p:Configuration=%BuildConfiguration% %*