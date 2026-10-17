# Remove previous results so old coverage files are not merged.
Remove-Item .\TestResults, .\coveragereport -Recurse -Force -ErrorAction SilentlyContinue

# Run tests and collect coverage using Coverlet settings.
dotnet test --collect:"XPlat Code Coverage" --settings .\coverlet.runsettings

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

# Generate report from the current test run only.
reportgenerator "-reports:TestResults/**/coverage.cobertura.xml" "-targetdir:coveragereport" "-reporttypes:TextSummary"

Get-Content .\coveragereport\Summary.txt
