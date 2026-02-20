# Project Guidelines

## Overview

This repository automates Chocolatey packaging of AWS Vault for Windows. It downloads pre-built binaries from upstream, generates package metadata, and tests installation in Docker.

## Architecture

**Hybrid Build Approach**:
- Package generation runs natively via PowerShell 7 ([New-ChocolateyPackage.ps1](../build/chocolatey/New-ChocolateyPackage.ps1))
- Testing and publishing run in Docker for isolation ([docker-compose.yml](../docker-compose.yml))

**Key Components**:
- [build.cake](../build.cake): Orchestrates the entire build pipeline using Cake Build
- [build/chocolatey/New-ChocolateyPackage.ps1](../build/chocolatey/New-ChocolateyPackage.ps1): Core package generator (downloads binary, calculates checksums, generates metadata)
- [build/chocolatey/package.json](../build/chocolatey/package.json): Package configuration (ID, title, URLs)
- Docker: Windows Server Core 2022 with Chocolatey 2.5.1 for clean testing

**Build Task Flow**: Init → Restore (Docker build) → Build (native package generation) → Package (Docker test) → Publish (Docker push)

## Build and Test

```powershell
# Restore .NET tools first
dotnet tool restore

# Build and package (default target)
dotnet cake

# Build with specific AWS Vault version
dotnet cake --source-version 7.10.0

# Test full workflow including install/uninstall
dotnet cake --target Package

# Publish (requires env vars)
$env:CHOCOLATEY_SERVER = "https://push.chocolatey.org/"
$env:CHOCOLATEY_API_KEY = "your-key"
dotnet cake --target Publish

# Clean artifacts and Docker resources
dotnet cake --target Clean
```

## Project Conventions

**Version Management** (4 distinct versions in [build.cake](../build.cake)):
- `source-version`: Upstream AWS Vault release (default: 7.9.5)
- `build-version`: Unix timestamp for build identification
- `project-version`: Version being packaged (= source-version)
- `package-version`: Chocolatey package version (= source-version, can differ for package iterations)

**Binary Verification**: Build task runs `aws-vault.exe --version` and validates output matches `v{project-version}` before proceeding.

**Embedded Binary Pattern**: Package includes the binary in `tools/` directory rather than downloading at install time. Requires `VERIFICATION.txt` with MD5/SHA256/SHA512 checksums for Chocolatey moderator review.

**No Install Scripts**: Package doesn't use `chocolateyInstall.ps1`. Chocolatey auto-shims executables found in `tools/`.

**Docker Command Wrapper**: All Docker operations use `RunDockerCommand()` helper in [build.cake](../build.cake#L11-L35) for consistent logging and error handling.

**Configuration Separation**: Package metadata lives in [package.json](../build/chocolatey/package.json), versions passed as arguments—enables updates without code changes.

## Integration Points

**Upstream Dependency**: Downloads from `https://github.com/ByteNess/aws-vault/releases/download/v{VERSION}/aws-vault-windows-amd64.exe`

**Chocolatey Publishing**: Uses environment variables `CHOCOLATEY_SERVER` and `CHOCOLATEY_API_KEY` for authentication. Publish target skips silently if server not configured.

**Docker Compose**: Mounts repository to `C:/opt/docker/work/` in container. Scripts run via `--entrypoint` override in [build.cake](../build.cake#L107-L119).

## CI/CD Setup

**GitHub Actions Workflow** ([.github/workflows/cd.yml](../.github/workflows/cd.yml)):
- Runs on `windows-2022` runners
- Triggers:
  - Push to `main` branch (auto-package)
  - Pull requests to `main` (validation)
  - Manual dispatch with optional publish flag

**Pipeline Steps**:
1. `dotnet tool restore` - Restores Cake 4.1.0 from [.config/dotnet-tools.json](../.config/dotnet-tools.json)
2. `dotnet cake --target package` - Builds and tests package
3. Uploads artifacts to GitHub (`.nupkg` files in `artifacts/chocolatey/`)
4. `dotnet cake --target publish --exclusive` - Publishes to Chocolatey (only if `publish: true` input provided)
5. `dotnet cake --target clean` - Cleanup (always runs)

**Required Secrets**:
- Organization variable: `CHOCOLATEY_SERVER` (e.g., `https://push.chocolatey.org/`)
- Repository secret: `CHOCOLATEY_API_KEY` (Chocolatey API key)

**Concurrency Control**: Cancels in-progress runs when new commits pushed to same branch.

## Local Development

**Prerequisites**:
- **Windows 10/11** or **Windows Server 2019+**
- **PowerShell 7.0+** (verify with `$PSVersionTable.PSVersion`)
- **.NET SDK 6.0+** (for `dotnet tool restore`)
- **Docker Desktop** with Windows containers enabled
- Git for version control

**Tool Versions** (auto-managed):
- Cake Build: 4.1.0 (via .NET local tool)
- Chocolatey: 2.5.1 (in Docker container via [Dockerfile](../build/chocolatey/Dockerfile))

**First-Time Setup**:
```powershell
# Clone repository
git clone https://github.com/gusztavvargadr/aws-vault-chocolatey.git
cd aws-vault-chocolatey

# Restore .NET tools (installs Cake 4.1.0)
dotnet tool restore

# Build Docker image (one-time, or after Dockerfile changes)
dotnet cake --target Restore
```

**Common Development Workflows**:

```powershell
# Quick build/test cycle (skips Docker)
dotnet cake --target Build --source-version 7.10.0

# Full test with Docker (install/uninstall verification)
dotnet cake --target Package --source-version 7.10.0

# Test specific version from scratch
dotnet cake --target Clean
dotnet cake --target Package --source-version 7.9.5

# Local publish test (requires env vars)
$env:CHOCOLATEY_SERVER = "http://localhost:8080"  # Local server
$env:CHOCOLATEY_API_KEY = "test-key"
dotnet cake --target Publish --source-version 7.9.5
```

**Troubleshooting**:
- **Docker errors**: Ensure Docker Desktop is running and switched to Windows containers
- **PowerShell version**: Script requires PS 7+, not Windows PowerShell 5.1
- **Binary download fails**: Check GitHub release exists at ByteNess/aws-vault for specified version
- **Version mismatch errors**: Clean artifacts with `dotnet cake --target Clean` and rebuild

## Code Style

**PowerShell**:
- Requires version 7.0+
- Use `Set-StrictMode -Version Latest` and `$ErrorActionPreference = 'Stop'`
- See [New-ChocolateyPackage.ps1](../build/chocolatey/New-ChocolateyPackage.ps1) for helper function patterns (Write-Log, Get-FileChecksums)
- Use CmdletBinding with parameter validation

**Cake Build**:
- Use helper functions for repeated operations (see `RunDockerCommand()`)
- Capture both stdout and stderr for Docker commands
- Check exit codes explicitly and throw exceptions on failure
