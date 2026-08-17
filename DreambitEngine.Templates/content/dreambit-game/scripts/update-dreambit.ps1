[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$')]
    [string] $SdkVersion,

    [string] $PackageSource
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$projectRoot = [System.IO.Path]::GetFullPath(
    [System.IO.Path]::Combine($PSScriptRoot, '..'))
$packageVersionsPath = Join-Path $projectRoot 'Directory.Packages.props'
$metadataPath = Join-Path $projectRoot '.dreambit/project.json'

foreach ($requiredPath in @($packageVersionsPath, $metadataPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "This is not a package-based Dreambit project: '$requiredPath' is missing."
    }
}

if (-not [string]::IsNullOrWhiteSpace($PackageSource)) {
    $PackageSource = [System.IO.Path]::GetFullPath($PackageSource)
    if (-not (Test-Path -LiteralPath $PackageSource -PathType Container)) {
        throw "Dreambit package source '$PackageSource' does not exist."
    }

    foreach ($packageId in @('DreambitEngine', 'Dreambit.Editor.Abstractions', 'DreambitEngine.Build')) {
        $packagePath = Join-Path $PackageSource "$packageId.$SdkVersion.nupkg"
        if (-not (Test-Path -LiteralPath $packagePath -PathType Leaf)) {
            throw "Dreambit package '$packageId' version '$SdkVersion' was not found in '$PackageSource'."
        }
    }
}

function Write-Utf8File {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Content
    )

    [System.IO.File]::WriteAllText(
        $Path,
        $Content,
        [System.Text.UTF8Encoding]::new($false))
}

function Set-DreambitPackageVersion {
    param([Parameter(Mandatory = $true)][string] $Content)

    foreach ($packageId in @('DreambitEngine', 'Dreambit.Editor.Abstractions', 'DreambitEngine.Build')) {
        $pattern = '(<PackageVersion\s+Include="' +
            [System.Text.RegularExpressions.Regex]::Escape($packageId) +
            '"\s+Version=")[^"]+("\s*/?>)'
        if (-not [System.Text.RegularExpressions.Regex]::IsMatch($Content, $pattern)) {
            throw "Directory.Packages.props does not contain a version entry for '$packageId'."
        }
        $Content = [System.Text.RegularExpressions.Regex]::Replace(
            $Content,
            $pattern,
            ('${1}' + $SdkVersion + '${2}'))
    }
    return $Content
}

$originalPackageVersions = [System.IO.File]::ReadAllText($packageVersionsPath)
$originalMetadata = [System.IO.File]::ReadAllText($metadataPath)

try {
    $updatedPackageVersions = Set-DreambitPackageVersion -Content $originalPackageVersions
    $metadata = $originalMetadata | ConvertFrom-Json
    if ($null -eq $metadata.sdk) {
        throw "Dreambit project metadata does not contain an SDK version."
    }
    if ([string]::IsNullOrWhiteSpace($metadata.solution)) {
        throw "Dreambit project metadata does not contain a solution path."
    }
    $metadata.sdk.version = $SdkVersion

    Write-Utf8File -Path $packageVersionsPath -Content $updatedPackageVersions
    Write-Utf8File -Path $metadataPath -Content ($metadata | ConvertTo-Json -Depth 10)

    $restoreArguments = @('restore', (Join-Path $projectRoot $metadata.solution), '--nologo')
    if (-not [string]::IsNullOrWhiteSpace($PackageSource)) {
        $restoreArguments += "-p:RestoreAdditionalProjectSources=$PackageSource"
    }

    & dotnet @restoreArguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore exited with code $LASTEXITCODE."
    }
}
catch {
    Write-Utf8File -Path $packageVersionsPath -Content $originalPackageVersions
    Write-Utf8File -Path $metadataPath -Content $originalMetadata
    throw
}

Write-Host "Updated this project to Dreambit SDK $SdkVersion." -ForegroundColor Green
