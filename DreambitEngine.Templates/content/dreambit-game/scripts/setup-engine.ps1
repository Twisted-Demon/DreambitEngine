[CmdletBinding()]
param(
    [string]$Repository = "__DREAMBIT_ENGINE_REPOSITORY__",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$engineRelativePath = "external/DreambitEngine"
$enginePath = Join-Path $root $engineRelativePath
$engineProject = Join-Path $enginePath "DreambitEngine/DreambitEngine.csproj"
$solution = Join-Path $root "DreambitGame.sln"
$launcher = Join-Path $root "src/DreambitGame.VK/DreambitGame.VK.csproj"

function Invoke-Checked {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Command)
    & $Command[0] $Command[1..($Command.Length - 1)]
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code $LASTEXITCODE: $($Command -join ' ')"
    }
}

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw "Git is required. Install Git and run this script again."
}

Push-Location $root
try {
    if (-not (Test-Path (Join-Path $root ".git"))) {
        Invoke-Checked git init
    }

    if (Test-Path $engineProject) {
        Write-Host "DreambitEngine already exists at $enginePath" -ForegroundColor Yellow
    }
    elseif (Test-Path $enginePath) {
        $items = @(Get-ChildItem $enginePath -Force -ErrorAction SilentlyContinue)
        if ($items.Count -gt 0) {
            throw "'$enginePath' exists but is not a valid DreambitEngine checkout. Move or delete it and rerun this script."
        }

        Remove-Item $enginePath -Force -Recurse
        Invoke-Checked git submodule add $Repository $engineRelativePath
        Invoke-Checked git submodule update --init --recursive
    }
    else {
        New-Item (Split-Path $enginePath -Parent) -ItemType Directory -Force | Out-Null
        Invoke-Checked git submodule add $Repository $engineRelativePath
        Invoke-Checked git submodule update --init --recursive
    }

    Invoke-Checked dotnet restore $solution

    if (-not $SkipBuild) {
        Invoke-Checked dotnet build $launcher
    }

    Write-Host "Dreambit Game is ready." -ForegroundColor Green
    Write-Host "Run it with: dotnet run --project src/DreambitGame.VK"
}
finally {
    Pop-Location
}
