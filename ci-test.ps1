$codecovProfile = 'Release'

# Set active path to script-location:
$path = $MyInvocation.MyCommand.Path
if (!$path) { $path = $psISE.CurrentFile.Fullpath }
if ($path) { $path = Split-Path $path -Parent }
Set-Location $path

$historyOutputFolder = "$path/history"

$coverageOutputFolder = "$path/coveragereport"
Get-ChildItem $coverageOutputFolder | Remove-Item -Recurse -Force

$reportOutputFolder = "$path/report"
Get-ChildItem $reportOutputFolder | Remove-Item -Recurse -Force

dotnet clean -c $codecovProfile

dotnet test --collect:"XPlat Code Coverage" --results-directory $coverageOutputFolder -c $codecovProfile /p:CodeCov=true
dotnet reportgenerator -reports:"$coverageOutputFolder/*/*.xml" -targetdir:$reportOutputFolder -reporttypes:Html -historydir:$historyOutputFolder
