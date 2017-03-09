#!bin/bash
set -e
dotnet restore
dotnet test test/doc-stack-app-api.tests/doc-stack-app-api.tests.csproj --logger:trx
cp -a test/doc-stack-app-api.tests/testresults/. $(pwd)/testresults
rm -rf $(pwd)/publish/web
dotnet publish src/doc-stack-app-api/doc-stack-app-api.csproj -c release -o $(pwd)/publish/web