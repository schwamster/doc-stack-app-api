#!bin/bash
set -e
dotnet restore
# dotnet test test/folder/project.json
rm -rf $(pwd)/publish/web
dotnet publish src/doc-stack-app-api/project.json -c release -o $(pwd)/publish/web