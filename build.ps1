[CmdletBinding(PositionalBinding = $false, DefaultParameterSetName = 'CreatePackages')]
param(
    [Parameter(
        ParameterSetName = 'CreatePackages'
    )]
    [switch]
    $CreatePackages,
    [Parameter(
        ParameterSetName = 'PublishPackages'
    )]
    [switch] $PublishPackages,
    [Parameter(
        ParameterSetName = 'PublishPackages'
    )]
    [string] $Destination
)

# Set active path to script-location:
$path = $MyInvocation.MyCommand.Path
if (!$path) { $path = $psISE.CurrentFile.Fullpath }
if ($path) { $path = Split-Path $path -Parent }
Set-Location $path

$nugetUri = "https://lat-devops.tlt.psu.edu/DefaultCollection/Library/_packaging/NET5/nuget/v3/index.json"
$output = & dotnet minver -t v
$packageVersion = $output
$packageOutputFolder = "$path/artifacts"

Write-Host "Run Parameters:" -ForegroundColor Cyan
Write-Host "  CreatePackages: $CreatePackages"
Write-Host "  PublishPackages: $PublishPackages"
Write-Host "  PackageVersion: $packageVersion"
Write-Host "  dotnet --version:" (dotnet --version)

Write-Host "Clean all projects..." -ForegroundColor "Magenta"
dotnet clean -c Release

Write-Host "Restoring all projects..." -ForegroundColor "Magenta"
dotnet restore
Write-Host "Done restoring." -ForegroundColor "Green"

Write-Host "Building all projects..." -ForegroundColor "Magenta"
dotnet build -c Release --no-restore /p:CI=true /p:Version=$packageVersion
Write-Host "Done building." -ForegroundColor "Green"

if ($CreatePackages -or $PublishPackages)
{
    New-Item -ItemType Directory -Path $packageOutputFolder -Force | Out-Null
    Write-Host "Clearing existing $packageOutputFolder..." -NoNewline
    Get-ChildItem $packageOutputFolder | Remove-Item
    Write-Host "done." -ForegroundColor "Green"

    Write-Host "Building all packages" -ForegroundColor "Green"
    Write-Host "Packing $project (dotnet pack)..." -ForegroundColor "Magenta"
    dotnet pack --no-build -c Release /p:PackageOutputPath=$packageOutputFolder /p:NoPackageAnalysis=true /p:CI=true /p:Version=$packageVersion /p:PublicRelease=true
    Write-Host ""
}

if ($PublishPackages)
{
    $packages = Get-ChildItem $packageOutputFolder -Name
    foreach ($package in $packages)
    {
        Write-Host "Publishing $package (dotnet nuget push)..." -ForegroundColor "Magenta"
        dotnet nuget push "$packageOutputFolder/$package" --skip-duplicate --source $nugetUri --api-key az
        Write-Host ""
    }
}
Write-Host "Build Complete." -ForegroundColor "Green"
