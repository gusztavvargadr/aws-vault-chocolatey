#!/usr/bin/env pwsh
<#
.SYNOPSIS
Detects the next missing upstream version that needs to be packaged.

.DESCRIPTION
Reads the current version from package.json, queries GitHub API for all
aws-vault releases, and returns the earliest version greater than current.

.PARAMETER PackageJsonPath
Path to the package.json file containing the current version.

.PARAMETER Repository
GitHub repository in format 'owner/repo'.

.PARAMETER Token
Optional GitHub API token for higher rate limits.

.OUTPUTS
The next missing version (e.g., "7.9.6") or empty string if no missing versions.
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$PackageJsonPath,

    [Parameter(Mandatory = $false)]
    [string]$Repository = "ByteNess/aws-vault",

    [Parameter(Mandatory = $false)]
    [string]$Token
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Host "Detecting next missing version..."

# Read current version
$packageJson = Get-Content $PackageJsonPath | ConvertFrom-Json
$currentVersion = [version]$packageJson.Version
Write-Host "Current version: $currentVersion"

# GitHub API setup
$apiUrl = "https://api.github.com/repos/$Repository/releases"
$headers = @{ "Accept" = "application/vnd.github.v3+json" }
if ($Token) {
    $headers["Authorization"] = "token $Token"
}

try {
    # Get all releases from GitHub API
    $releases = @()
    $page = 1
    
    do {
        Write-Host "Fetching releases page $page from $Repository..."
        $pageUrl = "$apiUrl`?page=$page&per_page=100"
        $response = Invoke-RestMethod -Uri $pageUrl -Headers $headers
        
        if ($response.Count -eq 0) {
            break
        }
        
        $releases += $response
        $page++
        
        # Prevent infinite loops on unexpected responses
        if ($page -gt 100) {
            throw "Exceeded max pages (100) while fetching releases"
        }
    } while ($response.Count -eq 100)
    
    Write-Host "Found $($releases.Count) total releases"
    
    # Parse versions and sort
    $versions = @()
    foreach ($release in $releases) {
        $tag = $release.tag_name
        # Remove 'v' prefix if present
        $cleanTag = $tag -replace '^v', ''
        
        try {
            $parsedVersion = [version]$cleanTag
            $versions += @{
                Original = $tag
                Clean = $cleanTag
                Parsed = $parsedVersion
            }
        }
        catch {
            Write-Host "Skipped invalid version tag: $tag"
        }
    }
    
    # Sort by parsed version descending
    $sortedVersions = $versions | Sort-Object { $_.Parsed } -Descending
    
    # Find first version greater than current
    $nextVersion = $sortedVersions | Where-Object { $_.Parsed -gt $currentVersion } | Select-Object -Last 1
    
    if ($nextVersion) {
        Write-Host "Next missing version: $($nextVersion.Clean)"
        Write-Output $nextVersion.Clean
    }
    else {
        Write-Host "No missing versions found (already at latest or ahead)"
        Write-Output ""
    }
}
catch {
    Write-Error "Failed to detect next version: $_"
    exit 1
}
