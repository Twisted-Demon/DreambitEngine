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
$templateHive = Join-Path $testRoot ".template-hive"
$testName = "Dreambit.TemplateSmokeTest"
$generated = Join-Path $testRoot $testName
$testRepository = "https://example.invalid/DreambitEngine.git"
$testFps = 144

Push-Location $root
try {
    Remove-Item $testRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item $testRoot -ItemType Directory | Out-Null

    dotnet pack $project -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw "Template package build failed." }

    $version = ([xml](Get-Content $project)).Project.PropertyGroup.Version | Select-Object -First 1
    $package = Join-Path $root "bin/$Configuration/DreambitEngine.Templates.$version.nupkg"
    if (-not (Test-Path -LiteralPath $package -PathType Leaf)) {
        throw "Expected template package was not created: $package"
    }

    dotnet new --debug:custom-hive $templateHive install $package --force
    if ($LASTEXITCODE -ne 0) { throw "Template installation failed." }

    Push-Location $testRoot
    try {
        dotnet new --debug:custom-hive $templateHive dreambit-game `
            -n $testName `
            --game-title "Template Smoke Test" `
            --engine-repository $testRepository `
            --target-fps $testFps `
            --no-update-check
        if ($LASTEXITCODE -ne 0) { throw "Template generation failed." }
    }
    finally {
        Pop-Location
    }

    $powerShellScripts = @(Get-ChildItem $generated -Filter "*.ps1" -Recurse)
    foreach ($script in $powerShellScripts) {
        $tokens = $null
        $parseErrors = $null
        [System.Management.Automation.Language.Parser]::ParseFile(
            $script.FullName,
            [ref]$tokens,
            [ref]$parseErrors
        ) | Out-Null

        if ($parseErrors.Count -gt 0) {
            $messages = $parseErrors.Message -join "; "
            throw "Generated PowerShell script '$($script.FullName)' has syntax errors: $messages"
        }
    }

    $expectedFiles = @(
        ".editorconfig",
        ".gitignore",
        "$testName.sln",
        "build/$testName.Content.targets",
        "scripts/setup-engine.ps1",
        "scripts/setup-engine.sh",
        "scripts/update-engine.ps1",
        "scripts/update-engine.sh",
        "src/$testName/$testName.csproj",
        "src/$testName.Content/$testName.Content.csproj",
        "src/$testName.VK/$testName.VK.csproj"
    )

    foreach ($relativePath in $expectedFiles) {
        $expectedPath = Join-Path $generated $relativePath
        if (-not (Test-Path -LiteralPath $expectedPath -PathType Leaf)) {
            throw "Generated template is missing '$relativePath'."
        }
    }

    if (Test-Path -LiteralPath (Join-Path $generated ".template.config")) {
        throw "Generated output contains the template authoring configuration."
    }

    $program = Get-Content -Raw -LiteralPath (Join-Path $generated "src/$testName.VK/Program.cs")
    if ($program -notmatch 'title: "Template Smoke Test"' -or
        $program -notmatch "Core\.SetTargetFps\($testFps\);") {
        throw "Generated game title or target FPS was not replaced correctly."
    }

    $setupScript = Get-Content -Raw -LiteralPath (Join-Path $generated "scripts/setup-engine.ps1")
    if ($setupScript -notmatch [regex]::Escape($testRepository)) {
        throw "Generated engine repository was not replaced correctly."
    }

    $textFiles = Get-ChildItem $generated -Recurse -File |
        Where-Object { $_.Extension -in ".cs", ".csproj", ".json", ".md", ".props", ".ps1", ".sh", ".sln", ".targets" }
    foreach ($textFile in $textFiles) {
        if ((Get-Content -Raw -LiteralPath $textFile.FullName) -match '__DREAMBIT_[A-Z_]+__') {
            throw "Generated output contains an unresolved placeholder in '$($textFile.FullName)'."
        }
    }

    $launcherProject = Join-Path $generated "src/$testName.VK/$testName.VK.csproj"
    [xml]$launcherXml = Get-Content $launcherProject
    $projectReferences = @(
        $launcherXml.Project.ItemGroup.ProjectReference |
            ForEach-Object { $_.Include } |
            Where-Object { $_ }
    )

    $expectedReference = "../$testName/$testName.csproj"
    if ($projectReferences.Count -ne 1 -or $projectReferences[0] -ne $expectedReference) {
        throw "Unexpected launcher ProjectReferences: $($projectReferences -join ', ')"
    }

    dotnet msbuild $launcherProject -getProperty:TargetFramework "-p:DreambitContentBuildEnabled=false" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Generated launcher failed MSBuild evaluation." }

    $solutionPath = Join-Path $generated "$testName.sln"
    $solutionProjects = @(dotnet sln $solutionPath list) | ForEach-Object { $_.Trim() -replace '\\', '/' }
    if ($LASTEXITCODE -ne 0) { throw "Generated solution is invalid." }

    $expectedSolutionProjects = @(
        "src/$testName/$testName.csproj",
        "src/$testName.Content/$testName.Content.csproj",
        "src/$testName.VK/$testName.VK.csproj",
        "external/DreambitEngine/DreambitEngine/DreambitEngine.csproj",
        "external/DreambitEngine/Dreambit.Content/Dreambit.Content.csproj",
        "external/DreambitEngine/DreambitEngine.AssetBaker/DreambitEngine.AssetBaker.csproj"
    )

    foreach ($expectedProject in $expectedSolutionProjects) {
        if ($solutionProjects -notcontains $expectedProject) {
            throw "Generated solution is missing '$expectedProject'."
        }
    }

    if ($EnginePath) {
        $resolvedEnginePath = (Resolve-Path -LiteralPath $EnginePath).Path
        dotnet build $launcherProject -c Debug "-p:DreambitEngineRoot=$resolvedEnginePath"
        if ($LASTEXITCODE -ne 0) { throw "Generated game build failed." }
    }

    if ($KeepOutput) {
        Write-Host "Template smoke test passed: $generated" -ForegroundColor Green
    }
    else {
        Write-Host "Template smoke test passed." -ForegroundColor Green
    }
}
finally {
    Pop-Location
    if (-not $KeepOutput) {
        Remove-Item $testRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
