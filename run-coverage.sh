#!/usr/bin/env bash
set -e
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
dotnet tool install -g dotnet-reportgenerator-globaltool || true
export PATH="$PATH:$HOME/.dotnet/tools"
reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:"Html;TextSummary"
cat coverage-report/Summary.txt
