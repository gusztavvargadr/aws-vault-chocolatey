#Requires -Version 7.0

<#
.SYNOPSIS
    Builds a Chocolatey package from configuration.

.DESCRIPTION
    Downloads binary, calculates checksums, generates templates, and creates package structure.
    Replaces Chef-based package generation with native PowerShell.

.PARAMETER ConfigPath
    Path to the package configuration JSON file.

.PARAMETER OutputDirectory
    Base directory where package artifacts will be created.

.PARAMETER ProjectVersion
    Version of the upstream project. Defaults to CHOCOLATEY_PROJECT_VERSION environment variable.

.PARAMETER PackageVersion
    Version of the Chocolatey package. Defaults to CHOCOLATEY_PACKAGE_VERSION environment variable.

.EXAMPLE
    .\New-ChocolateyPackage.ps1 -ConfigPath .\package.json -OutputDirectory .\artifacts\chocolatey\packages
    
.EXAMPLE
    $env:CHOCOLATEY_PROJECT_VERSION = '7.7.10'
    $env:CHOCOLATEY_PACKAGE_VERSION = '7.7.10'
    .\New-ChocolateyPackage.ps1 -ConfigPath .\package.json -OutputDirectory .\artifacts\chocolatey\packages
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ConfigPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory,

    [Parameter(Mandatory = $false)]
    [string]$ProjectVersion = $env:CHOCOLATEY_PROJECT_VERSION,

    [Parameter(Mandatory = $false)]
    [string]$PackageVersion = $env:CHOCOLATEY_PACKAGE_VERSION
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

#region Helper Functions

function Write-Log {
    param([string]$Message, [string]$Level = 'Info')
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $color = switch ($Level) {
        'Error' { 'Red' }
        'Warning' { 'Yellow' }
        'Success' { 'Green' }
        default { 'White' }
    }
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $color
}

function Get-FileChecksums {
    param([string]$Path)
    
    Write-Log "Calculating checksums for $Path"
    
    $md5 = (Get-FileHash -Path $Path -Algorithm MD5).Hash
    $sha256 = (Get-FileHash -Path $Path -Algorithm SHA256).Hash
    $sha512 = (Get-FileHash -Path $Path -Algorithm SHA512).Hash
    
    return @{
        MD5    = $md5
        SHA256 = $sha256
        SHA512 = $sha512
    }
}

function New-NuspecFile {
    param(
        [hashtable]$Config,
        [string]$OutputPath
    )
    
    Write-Log "Generating nuspec file: $OutputPath"
    
    $nuspec = @"
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2015/06/nuspec.xsd">
  <metadata>
    <id>$($Config.Id)</id>
    <version>$($Config.PackageVersion)</version>
    <packageSourceUrl>$($Config.PackageSourceUrl)</packageSourceUrl>
    <owners>Gusztav Varga</owners>
    <title>$($Config.Title)</title>
    <authors>ByteNess</authors>
    <projectUrl>https://99designs.com/blog/engineering/aws-vault/</projectUrl>
    <iconUrl>https://github.com/ByteNess.png</iconUrl>
    <copyright>2015 ByteNess</copyright>
    <licenseUrl>$($Config.ProjectLicenseUrl)</licenseUrl>
    <requireLicenseAcceptance>true</requireLicenseAcceptance>
    <projectSourceUrl>$($Config.ProjectSourceUrl)</projectSourceUrl>
    <docsUrl>$($Config.ProjectSourceUrl)blob/v$($Config.ProjectVersion)/README.md</docsUrl>
    <bugTrackerUrl>$($Config.ProjectSourceUrl)issues/</bugTrackerUrl>
    <tags>aws-vault ByteNess aws</tags>
    <summary>A tool to securely store and access AWS credentials in a development environment</summary>
    <description>
      AWS Vault is a tool to securely store and access AWS credentials in a development environment.

      AWS Vault stores IAM credentials in your operating system's secure keystore and then generates temporary credentials from those to expose to your shell and applications. It's designed to be complementary to the AWS CLI tools, and is aware of your profiles and configuration in ``~/.aws/config``.
    </description>
    <releaseNotes>$($Config.ProjectSourceUrl)releases/tag/v$($Config.ProjectVersion)/</releaseNotes>
  </metadata>
  <files>
    <file src="tools\**" target="tools" />
  </files>
</package>
"@
    
    Set-Content -Path $OutputPath -Value $nuspec -Encoding UTF8
}

function New-VerificationFile {
    param(
        [string]$DownloadUrl,
        [hashtable]$Checksums,
        [string]$OutputPath
    )
    
    Write-Log "Generating VERIFICATION.txt: $OutputPath"
    
    $verification = @"

VERIFICATION
Verification is intended to assist the Chocolatey moderators and community
in verifying that this package's contents are trustworthy.

Verify that the checksum of $DownloadUrl matches the following:

md5: $($Checksums.MD5)
sha256: $($Checksums.SHA256)
sha512: $($Checksums.SHA512)

"@
    
    Set-Content -Path $OutputPath -Value $verification -Encoding UTF8
}

function New-LicenseFile {
    param(
        [string]$ProjectLicenseUrl,
        [string]$OutputPath
    )
    
    Write-Log "Generating LICENSE.txt: $OutputPath"
    
    $license = @"

From: $ProjectLicenseUrl

LICENSE

The MIT License (MIT)

Copyright (c) 2015 ByteNess

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

"@
    
    Set-Content -Path $OutputPath -Value $license -Encoding UTF8
}

function Get-BinaryFile {
    param(
        [string]$Url,
        [string]$OutputPath
    )
    
    Write-Log "Downloading binary from: $Url"
    Write-Log "Destination: $OutputPath"
    
    # Ensure parent directory exists
    $parentDir = Split-Path -Parent $OutputPath
    if (-not (Test-Path $parentDir)) {
        New-Item -ItemType Directory -Path $parentDir -Force | Out-Null
    }
    
    # Download with progress
    $ProgressPreference = 'SilentlyContinue'
    try {
        Invoke-WebRequest -Uri $Url -OutFile $OutputPath -UseBasicParsing
        Write-Log "Download complete: $(Get-Item $OutputPath | Select-Object -ExpandProperty Length) bytes" -Level Success
    }
    finally {
        $ProgressPreference = 'Continue'
    }
}

#endregion

#region Main Script

try {
    Write-Log "Starting Chocolatey package build"
    Write-Log "Config: $ConfigPath"
    Write-Log "Output: $OutputDirectory"
    
    # Validate version parameters
    if ([string]::IsNullOrWhiteSpace($ProjectVersion)) {
        throw "ProjectVersion must be provided via parameter or CHOCOLATEY_PROJECT_VERSION environment variable"
    }
    if ([string]::IsNullOrWhiteSpace($PackageVersion)) {
        throw "PackageVersion must be provided via parameter or CHOCOLATEY_PACKAGE_VERSION environment variable"
    }
    
    Write-Log "Project Version: $ProjectVersion"
    Write-Log "Package Version: $PackageVersion"
    
    # Load configuration
    if (-not (Test-Path $ConfigPath)) {
        throw "Configuration file not found: $ConfigPath"
    }
    
    $config = Get-Content $ConfigPath -Raw | ConvertFrom-Json -AsHashtable
    
    # Add version information to config
    $config.ProjectVersion = $ProjectVersion
    $config.PackageVersion = $PackageVersion
    $config.ProjectLicenseUrl = "$($config.ProjectSourceUrl)blob/v$ProjectVersion/LICENSE"
    
    Write-Log "Loaded configuration for package: $($config.Id) v$($config.PackageVersion)"
    
    # Create package directory structure
    $packageDir = Join-Path $OutputDirectory $config.Id
    $toolsDir = Join-Path $packageDir 'tools'
    
    Write-Log "Creating package directory: $packageDir"
    New-Item -ItemType Directory -Path $packageDir -Force | Out-Null
    New-Item -ItemType Directory -Path $toolsDir -Force | Out-Null
    
    # Build download URL
    $downloadUrl = "$($config.ProjectSourceUrl)releases/download/v$($config.ProjectVersion)/$($config.Id)-windows-amd64.exe"
    $binaryPath = Join-Path $toolsDir "$($config.Id).exe"
    
    # Download binary
    Get-BinaryFile -Url $downloadUrl -OutputPath $binaryPath
    
    # Calculate checksums
    $checksums = Get-FileChecksums -Path $binaryPath
    
    # Generate package files
    $nuspecPath = Join-Path $packageDir "$($config.Id).nuspec"
    New-NuspecFile -Config $config -OutputPath $nuspecPath
    
    $verificationPath = Join-Path $toolsDir 'VERIFICATION.txt'
    New-VerificationFile -DownloadUrl $downloadUrl -Checksums $checksums -OutputPath $verificationPath
    
    $licensePath = Join-Path $toolsDir 'LICENSE.txt'
    New-LicenseFile -ProjectLicenseUrl $config.ProjectLicenseUrl -OutputPath $licensePath
    
    Write-Log "Package structure created successfully at: $packageDir" -Level Success
    Write-Log "Files created:" -Level Success
    Write-Log "  - $nuspecPath" -Level Success
    Write-Log "  - $binaryPath" -Level Success
    Write-Log "  - $verificationPath" -Level Success
    Write-Log "  - $licensePath" -Level Success
    
    # Return package directory for further processing
    return $packageDir
}
catch {
    Write-Log "Error: $($_.Exception.Message)" -Level Error
    Write-Log "Stack trace: $($_.ScriptStackTrace)" -Level Error
    throw
}

#endregion
