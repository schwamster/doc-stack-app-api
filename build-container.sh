#!bin/bash
set -e
dotnet restore
dotnet test test/doc-stack-app-api.tests/doc-stack-app-api.tests.csproj -xml $(pwd)/testresults/out.xml
rm -rf $(pwd)/publish/web
dotnet publish src/doc-stack-app-api/doc-stack-app-api.csproj -c release -o $(pwd)/publish/web