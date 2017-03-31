dotnet restore
dotnet build --configuration=release src\Microsoft.Web.Xdt\Microsoft.Web.Xdt.csproj
dotnet pack --configuration=release src\Microsoft.Web.Xdt\Microsoft.Web.Xdt.csproj