dotnet new tool-manifest
dotnet tool install dotnet-reportgenerator-globaltool

dotnet test --collect:"XPlat Code Coverage" --results-directory coveragereport
dotnet reportgenerator -reports:coveragereport\*\*.xml -targetdir:report -reporttypes:Html
