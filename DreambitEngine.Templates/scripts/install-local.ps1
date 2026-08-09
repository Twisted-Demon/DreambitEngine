[CmdletBinding()]
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "DreambitEngine.Templates.csproj"

Push-Location $root
try {
    dotnet pack $project -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw "Template package build failed." }

    $version = ([xml](Get-Content $project)).Project.PropertyGroup.Version | Select-Object -First 1
    $package = Join-Path $root "bin/$Configuration/DreambitEngine.Templates.$version.nupkg"

    dotnet new install $package --force
    if ($LASTEXITCODE -ne 0) { throw "Template installation failed." }

    Write-Host "Installed DreambitEngine.Templates $version." -ForegroundColor Green
    Write-Host 'Create a game with: dotnet new dreambit-game -n MyGame --game-title "My Game"'
}
finally {
    Pop-Location
}
