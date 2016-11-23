#!bin/bash
set -e
dotnet restore
dotnet test test/doc-stack-app-api.tests/project.json -xml $(pwd)/testresults/out.xml
rm -rf $(pwd)/publish/web
dotnet publish src/doc-stack-app-api/project.json -c release -o $(pwd)/publish/web