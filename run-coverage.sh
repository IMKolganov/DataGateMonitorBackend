#!/usr/bin/env bash
# Code coverage via coverlet (exclusions from coverlet.runsettings apply).
# Use when Rider/dotCover filters don't work — report excludes Migrations and SharedModels.

set -e
cd "$(dirname "$0")"

dotnet test DataGateMonitor.Tests/DataGateMonitor.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --settings coverlet.runsettings \
  --results-directory TestResults/Coverlet

echo ""
echo "Done. Coverage files: TestResults/Coverlet/**/coverage.cobertura.xml"
echo "HTML report: dotnet reportgenerator -reports:TestResults/Coverlet/**/coverage.cobertura.xml -targetdir:TestResults/CoverageReport -reporttypes:Html"
echo "(requires: dotnet tool install -g dotnet-reportgenerator-globaltool)"
