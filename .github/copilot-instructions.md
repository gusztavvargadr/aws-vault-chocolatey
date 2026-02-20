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
- [build/chocolatey/package.json](../build/chocolatey/package.json): Package configuration and centralized version storage
- [lib/ByteNess/aws-vault](../lib/ByteNess/aws-vault): Git submodule tracking upstream releases
- [build/Get-NextVersion.ps1](../build/Get-NextVersion.ps1): Detects next missing upstream version via GitHub API
- [build/New-ReleaseNotes.ps1](../build/New-ReleaseNotes.ps1): Generates release notes with git changelog
- [build/release-template.md](../build/release-template.md): Release notes template with version placeholders
- Docker: Windows Server Core 2022 with Chocolatey 2.5.1 for clean testing

**Build Task Flow**: Init → Restore (Docker build) → Build (native package generation) → Package (Docker test) → Publish (Docker push)

**Release Workflow**: Update Submodule (detects new version) → PR (for review) → CD (build/test) → Tag push → Release (publish + GitHub release)

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

# Generate release notes for a version
dotnet cake --target GenerateReleaseNotes --release-version 7.9.6 --release-previous-version 7.9.5

# Publish (requires env vars)
$env:CHOCOLATEY_SERVER = "https://push.chocolatey.org/"
$env:CHOCOLATEY_API_KEY = "your-key"
dotnet cake --target Publish

# Clean artifacts and Docker resources
dotnet cake --target Clean
```

## Project Conventions

**Version Management**: Version now stored in [build/chocolatey/package.json](../build/chocolatey/package.json) as the source of truth. [build.cake](../build.cake) reads this value instead of hardcoding. This enables:
- Single version entry point for all automation
- Easy updates without modifying build scripts
- Workflow-friendly version tracking

**Version Build Arguments** (still supported for manual testing):
- `--source-version`: Upstream AWS Vault release (overrides package.json default)
- `--build-version`: Unix timestamp for build identification
- `--project-version`: Version being packaged (= source-version)
- `--package-version`: Chocolatey package version (= source-version)

**Upstream Tracking**: [lib/ByteNess/aws-vault](../lib/ByteNess/aws-vault) is a git submodule that points to the upstream aws-vault repository. The Update Submodule workflow updates this to track specific release tags.

**Release Notes**:
- Template: [build/release-template.md](../build/release-template.md) with placeholders (`{{VERSION}}`, `{{PREVIOUS_VERSION}}`, `{{AUTHOR}}`, `{{CHANGELOG}}`)
- Generator: [build/New-ReleaseNotes.ps1](../build/New-ReleaseNotes.ps1) performs variable substitution and generates changelog from git history
- Cake Task: `GenerateReleaseNotes` task generates final release notes to `artifacts/release-notes.md`

**Binary Verification**: Build task runs `aws-vault.exe --version` and validates output matches `v{project-version}` before proceeding.

**Embedded Binary Pattern**: Package includes the binary in `tools/` directory rather than downloading at install time. Requires `VERIFICATION.txt` with MD5/SHA256/SHA512 checksums for Chocolatey moderator review.

**No Install Scripts**: Package doesn't use `chocolateyInstall.ps1`. Chocolatey auto-shims executables found in `tools/`.

**Docker Command Wrapper**: All Docker operations use `RunDockerCommand()` helper in [build.cake](../build.cake) for consistent logging and error handling.

## Automated Release Processing

**Release Workflow Overview**:
1. **Check for Updates workflow** (daily or manual):
   - Queries GitHub API for aws-vault releases
   - Detects next missing version (not latest, enables sequential processing)
   - Auto-creates PR with version bump in package.json
   - Updates submodule to that release tag
   
2. **CD workflow** (on PR/push to main):
   - Validates package builds successfully
   - Tests install/uninstall in Docker
   - Uploads artifacts for review
   
3. **Release workflow** (on tag creation `v*`):
   - Generates release notes with git changelog
   - Publishes to Chocolatey (if env vars configured)
   - Creates GitHub release with generated notes
   
**Sequential Version Processing**:
- [Get-NextVersion.ps1](../build/Get-NextVersion.ps1) compares current version (in package.json) against all GitHub releases
- Returns only the earliest missing version (e.g., if at 7.9.5, returns 7.9.6, not 7.9.7)
- Prevents version skipping; enables sequential release of backlogs
- Workflow re-runs after each tag push to detect and process next version

**Release Notes Generation**:
- Template: [release-template.md](../build/release-template.md)
- Generator: [New-ReleaseNotes.ps1](../build/New-ReleaseNotes.ps1)
- Cake task: `GenerateReleaseNotes --release-version X --release-previous-version Y`
- Includes git changelog (commits between tags) in the overview section

## Integration Points

**Upstream Dependency**: Downloads from `https://github.com/ByteNess/aws-vault/releases/download/v{VERSION}/aws-vault-windows-amd64.exe`

**Upstream Tracking**: Submodule at [lib/ByteNess/aws-vault](../lib/ByteNess/aws-vault) points to `https://github.com/ByteNess/aws-vault`. Update Submodule workflow fetches releases and updates to specific tags.

**Chocolatey Publishing**: Uses environment variables `CHOCOLATEY_SERVER` and `CHOCOLATEY_API_KEY` for authentication. Publish target skips silently if server not configured.

**Docker Compose**: Mounts repository to `C:/opt/docker/work/` in container. Scripts run via `--entrypoint` override in [build.cake](../build.cake).

## CI/CD Setup

**GitHub Actions Workflows**:

**Continuous Delivery** ([.github/workflows/cd.yml](../.github/workflows/cd.yml)):
- Runs on `windows-2022` runners
- Triggers: Push to `main` branch or PR to `main`
- Steps:
  1. `dotnet tool restore` - Restores Cake 4.1.0
  2. `dotnet cake --target package` - Builds and tests package
  3. Uploads `.nupkg` artifacts to GitHub
  4. `dotnet cake --target clean` - Cleanup (always runs)

**Check for Updates** ([.github/workflows/check-for-updates.yml](../.github/workflows/check-for-updates.yml)):
- Runs on schedule (daily at 9 AM UTC) or manual dispatch
- Steps:
  1. Calls `Get-NextVersion.ps1` to detect next missing version (not latest)
  2. Updates submodule to that specific release tag
  3. Updates version in [package.json](../build/chocolatey/package.json)
  4. Creates PR with changes (labels: automation, dependencies)
  5. If no missing versions, exits gracefully
- Enables sequential processing of multiple releases (one PR per version)

**Release** ([.github/workflows/release.yml](../.github/workflows/release.yml)):
- Triggered on tag creation (pattern: `v*`)
- Steps:
  1. Extracts version from tag
  2. Finds previous release tag
  3. Calls `GenerateReleaseNotes` Cake task (generates release notes with git changelog)
  4. `dotnet cake --target package` - Builds final package
  5. `dotnet cake --target publish --exclusive` - Publishes to Chocolatey
  6. Creates GitHub release with generated release notes
  7. `dotnet cake --target clean` - Cleanup

**Required Secrets/Variables**:
- Organization variable: `CHOCOLATEY_SERVER` (e.g., `https://push.chocolatey.org/`)
- Repository secret: `CHOCOLATEY_API_KEY` (Chocolatey API key)

**Concurrency Control**: All workflows cancel in-progress runs when new commits pushed to same branch.

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

# Generate release notes locally
dotnet cake --target GenerateReleaseNotes --release-version 7.9.6 --release-previous-version 7.9.5

# Test release notes with custom author (defaults to aws-vault-chocolatey)
dotnet cake --target GenerateReleaseNotes --release-version 7.9.6 --release-previous-version 7.9.5 --release-author "your-name"

# Local publish test (requires env vars)
$env:CHOCOLATEY_SERVER = "http://localhost:8080"  # Local server
$env:CHOCOLATEY_API_KEY = "test-key"
dotnet cake --target Publish --source-version 7.9.5

# Trigger version detection workflow manually
gh workflow run check-for-updates.yml
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
