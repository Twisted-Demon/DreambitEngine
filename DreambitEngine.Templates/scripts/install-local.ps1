[CmdletBinding()]
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$templateRoot = Split-Path -Parent $PSScriptRoot
$engineRoot = Split-Path -Parent $templateRoot
$templateProject = Join-Path $templateRoot "DreambitEngine.Templates.csproj"
$version = ([xml](Get-Content $templateProject)).Project.PropertyGroup.Version | Select-Object -First 1
$localData = if ($env:LOCALAPPDATA) { $env:LOCALAPPDATA } else { $HOME }
$feed = Join-Path $localData "Dreambit/Editor/sdks/$version/packages"

New-Item $feed -ItemType Directory -Force | Out-Null
foreach ($project in @(
    (Join-Path $engineRoot "DreambitEngine/DreambitEngine.csproj"),
    (Join-Path $engineRoot "DreambitEngine.Build/DreambitEngine.Build.csproj"),
    $templateProject
)) {
    dotnet pack $project -c $Configuration "-p:PackageVersion=$version" -o $feed --nologo
    if ($LASTEXITCODE -ne 0) { throw "SDK package build failed for '$project'." }
}

$templatePackage = Join-Path $feed "DreambitEngine.Templates.$version.nupkg"
dotnet new install $templatePackage --force
if ($LASTEXITCODE -ne 0) { throw "Template installation failed." }

Write-Host "Installed Dreambit SDK $version at $feed." -ForegroundColor Green
Write-Host 'Create a game with: dotnet new dreambit-game -n MyGame --game-title "My Game"'
