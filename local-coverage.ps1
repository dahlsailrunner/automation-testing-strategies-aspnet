gci -include TestResults,coverage -recurse | remove-item -force -recurse
dotnet test --settings tests/carvedrock.runsettings
reportgenerator -reports:".\Tests\**\TestResults\**\coverage.cobertura.xml" -targetdir:"coverage" -reporttypes:Html
.\coverage\index.htm
