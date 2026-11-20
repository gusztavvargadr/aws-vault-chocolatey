#!/usr/bin/env pwsh
<#
.SYNOPSIS
Moves the packaged upstream version on to another release.

.DESCRIPTION
Fetches the upstream tags into the submodule, checks it out at the version's
v* tag, and sets the version in package.json. Git's own output goes to the
host, so the previous version is the only thing the script returns.

.PARAMETER Version
The upstream version to move to, without the v prefix.

.PARAMETER PackageJsonPath
Path to the package.json file that states the upstream version.

.PARAMETER SubmodulePath
Path to the upstream submodule.

.OUTPUTS
The version package.json stated before the change (e.g., "7.18.1").
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$PackageJsonPath,

    [Parameter(Mandatory = $true)]
    [string]$SubmodulePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$tag = "v$Version"

Write-Host "Fetching tags in $SubmodulePath..."
git -C $SubmodulePath fetch --all --tags 2>&1 | Out-Host
if ($LASTEXITCODE -ne 0) {
    throw "Fetching tags in '$SubmodulePath' failed with exit code $LASTEXITCODE."
}

Write-Host "Checking out $tag in $SubmodulePath..."
git -C $SubmodulePath checkout $tag 2>&1 | Out-Host
if ($LASTEXITCODE -ne 0) {
    throw "Checking out '$tag' in '$SubmodulePath' failed with exit code $LASTEXITCODE."
}

$packageJson = Get-Content -Raw $PackageJsonPath | ConvertFrom-Json
$previousVersion = $packageJson.Version
$packageJson.Version = $Version
$packageJson | ConvertTo-Json -Depth 10 | Set-Content $PackageJsonPath -Encoding utf8
Write-Host "Moved $PackageJsonPath from $previousVersion to $Version"

Write-Output $previousVersion
