[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$EnginePath,
    [switch]$KeepOutput
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "DreambitEngine.Templates.csproj"
$testRoot = Join-Path $root "TemplateTests"
$generated = Join-Path $testRoot "TemplateSmokeTest"

Push-Location $root
try {
    Remove-Item $testRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item $testRoot -ItemType Directory | Out-Null

    dotnet pack $project -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw "Template package build failed." }

    $version = ([xml](Get-Content $project)).Project.PropertyGroup.Version | Select-Object -First 1
    $package = Join-Path $root "bin/$Configuration/DreambitEngine.Templates.$version.nupkg"
    dotnet new install $package --force
    if ($LASTEXITCODE -ne 0) { throw "Template installation failed." }

    Push-Location $testRoot
    try {
        dotnet new dreambit-game -n TemplateSmokeTest --game-title "Template Smoke Test"
        if ($LASTEXITCODE -ne 0) { throw "Template generation failed." }
    }
    finally {
        Pop-Location
    }

    $launcherProject = Join-Path $generated "src/TemplateSmokeTest.VK/TemplateSmokeTest.VK.csproj"
    [xml]$launcherXml = Get-Content $launcherProject
    $projectReferences = @(
        $launcherXml.Project.ItemGroup.ProjectReference |
            ForEach-Object { $_.Include } |
            Where-Object { $_ }
    )

    $expectedReference = "../TemplateSmokeTest/TemplateSmokeTest.csproj"
    if ($projectReferences.Count -ne 1 -or $projectReferences[0] -ne $expectedReference) {
        throw "Unexpected launcher ProjectReferences: $($projectReferences -join ', ')"
    }

    if ($EnginePath) {
        $external = Join-Path $generated "external/DreambitEngine"
        New-Item (Split-Path $external -Parent) -ItemType Directory -Force | Out-Null
        Copy-Item $EnginePath $external -Recurse
        dotnet build (Join-Path $generated "src/TemplateSmokeTest.VK/TemplateSmokeTest.VK.csproj") -c Debug
        if ($LASTEXITCODE -ne 0) { throw "Generated game build failed." }
    }

    Write-Host "Template smoke test passed: $generated" -ForegroundColor Green
}
finally {
    Pop-Location
    if (-not $KeepOutput) {
        Remove-Item $testRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
