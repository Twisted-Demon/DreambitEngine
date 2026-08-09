[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Push-Location $root
try {
    git submodule update --init --recursive --remote external/DreambitEngine
    if ($LASTEXITCODE -ne 0) { throw "DreambitEngine update failed." }

    dotnet restore "DreambitGame.sln"
    if ($LASTEXITCODE -ne 0) { throw "Restore failed after updating DreambitEngine." }

    Write-Host "DreambitEngine updated." -ForegroundColor Green
}
finally {
    Pop-Location
}
