$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$enginePath = Join-Path $projectRoot ".dreambit\engine"
$repository = "__DREAMBIT_ENGINE_REPOSITORY__"
$engineRef = "__DREAMBIT_ENGINE_REF__"

function Invoke-Git {
    param([string[]]$Arguments)

    & git @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "git failed with exit code ${LASTEXITCODE}: $($Arguments -join ' ')"
    }
}

Push-Location $projectRoot
try {
    if (-not (Test-Path ".git")) {
        Invoke-Git -Arguments @("init")
    }

    if (-not (Test-Path (Join-Path $enginePath "DreambitEngine\DreambitEngine.csproj"))) {
        Invoke-Git -Arguments @(
            "-c", "protocol.file.allow=always", "clone", "--no-checkout", $repository, $enginePath)
    }

    if (-not [string]::IsNullOrWhiteSpace($engineRef)) {
        Invoke-Git -Arguments @("-C", $enginePath, "fetch", "origin", $engineRef)
        Invoke-Git -Arguments @("-C", $enginePath, "checkout", "--detach", "FETCH_HEAD")
    }

    # A .gitmodules entry plus a staged nested repository is the same gitlink
    # layout produced by `git submodule add`, without relying on Git's shell
    # helper scripts being available on the host PATH.
    Invoke-Git -Arguments @(
        "config", "-f", ".gitmodules", "submodule.dreambit-engine.path", ".dreambit/engine")
    Invoke-Git -Arguments @(
        "config", "-f", ".gitmodules", "submodule.dreambit-engine.url", $repository)
    Invoke-Git -Arguments @("add", "--", ".gitmodules")
    $engineCommit = (Invoke-Git -Arguments @("-C", $enginePath, "rev-parse", "HEAD") |
        Select-Object -Last 1).Trim()
    Invoke-Git -Arguments @(
        "update-index", "--add", "--cacheinfo", "160000,$engineCommit,.dreambit/engine")
}
finally {
    Pop-Location
}
