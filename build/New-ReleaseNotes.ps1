#!/usr/bin/env pwsh
<#
.SYNOPSIS
Generates release notes from a template.

.DESCRIPTION
Reads a release notes template and performs variable substitution with the
provided parameters (version, previous version, and author).

.PARAMETER TemplatePath
Path to the release notes template file.

.PARAMETER Version
The current version being released.

.PARAMETER PreviousVersion
The previous version for changelog comparison.

.PARAMETER Author
The author/contributor name for the release (optional).

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

    [Parameter(Mandatory = $true)]
    [string]$PreviousVersion,

    [Parameter(Mandatory = $false)]
    [string]$Author = "aws-vault-chocolatey",

    [Parameter(Mandatory = $false)]
    [string]$Changelog,

    [Parameter(Mandatory = $false)]
    [string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Host "Generating release notes from template..."

# Read template
if (-not (Test-Path $TemplatePath)) {
    throw "Template file not found: $TemplatePath"
}

$template = Get-Content $TemplatePath -Raw

# Generate changelog if not provided
if ([string]::IsNullOrWhiteSpace($Changelog)) {
    Write-Host "Generating changelog from git history..."
    
    if ($PreviousVersion -eq "initial") {
        # Initial release - show all commits
        Write-Host "Generating changelog for initial release..."
        try {
            $commits = git log --pretty=format:"- %s (%h)" 2>$null
            if ($commits) {
                $Changelog = $commits
            } else {
                $Changelog = "Initial release"
            }
        }
        catch {
            Write-Warning "Failed to generate changelog: $_"
            $Changelog = "Initial release"
        }
    }
    else {
        # Compare against previous version
        Write-Host "Generating changelog between v$PreviousVersion and v$Version..."
        try {
            # Try to find commits between the tags
            $commits = git log "v$PreviousVersion..v$Version" --pretty=format:"- %s (%h)" 2>$null
            if (-not $commits) {
                # If no commits found between tags, try without v prefix
                $commits = git log "$PreviousVersion..$Version" --pretty=format:"- %s (%h)" 2>$null
            }
            if ($commits) {
                $Changelog = $commits
            } else {
                Write-Host "No commits found between v$PreviousVersion and v$Version"
                $Changelog = "See full changelog in resources below"
            }
        }
        catch {
            Write-Warning "Failed to generate changelog: $_"
            $Changelog = "See full changelog in resources below"
        }
    }
}

# Perform variable substitution
$releaseNotes = $template `
    -replace '{{VERSION}}', $Version `
    -replace '{{PREVIOUS_VERSION}}', $PreviousVersion `
    -replace '{{AUTHOR}}', $Author `
    -replace '{{CHANGELOG}}', $Changelog

if ($OutputPath) {
    Write-Host "Writing release notes to: $OutputPath"
    $releaseNotes | Out-File -FilePath $OutputPath -Encoding utf8
}
else {
    Write-Output $releaseNotes
}
