#!/usr/bin/env pwsh
<#
.SYNOPSIS
Generates the release notes for a version from a template and GitHub's generated release notes.

.DESCRIPTION
Finds the previous version as the highest v* tag below the version, asks GitHub to
generate the release notes between that tag and the commit, and fills the template
with them. GitHub's generated notes go in as they are, except their Full Changelog
line, which the template states instead. Calling GitHub needs gh, signed in or given
a token in GH_TOKEN.

.PARAMETER TemplatePath
Path to the release notes template file.

.PARAMETER Version
The version being released, without the v prefix.

.PARAMETER Commit
The commit the release's tag points at. Defaults to HEAD.

.PARAMETER OutputPath
Path where the generated release notes will be written.

.OUTPUTS
Writes the generated release notes to the specified output path.
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$TemplatePath,

    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $false)]
    [string]$Commit = "HEAD",

    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path $TemplatePath)) {
    throw "Template file not found: $TemplatePath"
}

$template = Get-Content $TemplatePath -Raw

$parsedVersion = [System.Version]::Parse($Version)

$commitSha = git rev-parse $Commit
if ($LASTEXITCODE -ne 0) {
    throw "Failed to resolve commit: $Commit"
}

$tags = @(git tag -l 'v*')
if ($LASTEXITCODE -ne 0) {
    throw "Failed to list tags."
}

$previousVersion = $tags |
    ForEach-Object {
        $tagVersion = $null
        if ([System.Version]::TryParse($_.Substring(1), [ref]$tagVersion) -and $tagVersion -lt $parsedVersion) {
            $tagVersion
        }
    } |
    Sort-Object -Descending |
    Select-Object -First 1

if (-not $previousVersion) {
    throw "No v* tag found below version $Version."
}

Write-Host "Generating release notes for v$Version at $commitSha since v$previousVersion..."

$generatedNotes = gh api 'repos/{owner}/{repo}/releases/generate-notes' -f "tag_name=v$Version" -f "target_commitish=$commitSha" -f "previous_tag_name=v$previousVersion" --jq '.body'
if ($LASTEXITCODE -ne 0) {
    throw "Failed to generate release notes from GitHub."
}

$contributions = (($generatedNotes -join "`n") -replace '(?m)^\*\*Full Changelog\*\*: .*$', '').Trim()

$releaseNotes = ($template.
    Replace('{{VERSION}}', $Version).
    Replace('{{PREVIOUS_VERSION}}', "$previousVersion").
    Replace('{{CONTRIBUTIONS}}', $contributions))

Write-Host "Writing release notes to: $OutputPath"
$releaseNotes | Out-File -FilePath $OutputPath -Encoding utf8 -NoNewline
