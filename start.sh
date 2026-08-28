#!/bin/bash
set -e
echo "Publishing solution..."
dotnet publish Driventa.API/Driventa.API.csproj -c Release -o ./publish
echo "Starting API..."
exec dotnet ./publish/Driventa.API.dll
