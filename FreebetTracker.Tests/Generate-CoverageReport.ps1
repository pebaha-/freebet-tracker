dotnet test --collect:"XPlat Code Coverage"
reportgenerator "-reports:TestResults/**/coverage.cobertura.xml" "-targetdir:coveragereport" "-reporttypes:TextSummary"
Get-Content .\coveragereport\Summary.txt
