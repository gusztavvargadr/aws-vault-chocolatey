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

**Release Workflow**: Update Submodule (detects new version) → PR (for review) → CD (build/test + artifact upload) → Tag push → Release (publish from CD artifact + GitHub release)

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

# Generate draft release notes (uses recent commits, no tags required)
dotnet cake --target GenerateDraftReleaseNotes --release-version 7.9.6 --release-previous-version 7.9.5

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
- Cake Task: `GenerateDraftReleaseNotes` generates release notes using recent commits (no git tags required)

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
   - Enables auto-merge on PR (requires `GH_PAT` secret, uses squash merge)
   
2. **CD workflow** (on PR/push to main):
   - Validates package builds successfully
   - Tests install/uninstall in Docker
   - Uploads artifacts for review (30 day retention)
   - **On main branch**: Creates draft GitHub release with full release notes (tag not created yet)
   
3. **Manual Review**:
   - Review draft release at `https://github.com/gusztavvargadr/aws-vault-chocolatey/releases`
   - Manually publish draft in GitHub UI → creates tag → triggers Release workflow
   
4. **Release workflow** (on tag creation `v*`):
   - Downloads the CD artifact for the same commit and validates the `.nupkg`
   - Publishes to Chocolatey (if env vars configured)
   - Publishes the draft release (or creates new release if draft missing)
   
**Sequential Version Processing**:
- [Get-NextVersion.ps1](../build/Get-NextVersion.ps1) compares current version (in package.json) against all GitHub releases
- Returns only the earliest missing version (e.g., if at 7.9.5, returns 7.9.6, not 7.9.7)
- Prevents version skipping; enables sequential release of backlogs
- Auto-merge ensures PRs are automatically merged after CD passes (when `GH_PAT` configured)
- Workflow re-runs after each merge/tag to detect and process next version

**Release Notes Generation**:
- Template: [release-template.md](../build/release-template.md)
- Generator: [New-ReleaseNotes.ps1](../build/New-ReleaseNotes.ps1)
- Cake Task: `GenerateDraftReleaseNotes` used by CD workflow to create draft releases with notes (uses recent 20 commits, no git tags required)
- Parses commit messages to extract PR information (format: "Title (#123) (Author)")
- Includes git-based changelog in the overview section

**Auto-Merge Configuration**:
- Requires `GH_PAT` secret with `repo` and `workflow` permissions
- Falls back gracefully if `GH_PAT` not configured (PRs still created, manual merge required)
- Uses squash merge strategy for clean git history
- Requires branch protection on `main` with required status checks (CD workflow)

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
- Permissions: `contents: write`, `pull-requests: read`
- Steps:
  1. `dotnet tool restore` - Restores Cake 4.1.0
  2. `dotnet cake --target package` - Builds and tests package
  3. Uploads `.nupkg` artifacts to GitHub
  4. **On main branch only**: Reads version, finds previous version, generates release notes, creates draft release
  5. `dotnet cake --target clean` - Cleanup (always runs)

**Check for Updates** ([.github/workflows/check-for-updates.yml](../.github/workflows/check-for-updates.yml)):
- Runs on schedule (daily at 9 AM UTC) or manual dispatch
- Permissions: `contents: write`, `pull-requests: write`
- Steps:
  1. Calls `Get-NextVersion.ps1` to detect next missing version (not latest)
  2. Updates submodule to that specific release tag
  3. Updates version in [package.json](../build/chocolatey/package.json)
  4. Creates PR with changes (labels: automation, dependencies)
  5. Enables auto-merge with squash strategy (if `GH_PAT` configured)
  6. If no missing versions, exits gracefully
- Enables sequential processing of multiple releases (one PR per version)

**Release** ([.github/workflows/release.yml](../.github/workflows/release.yml)):
- Triggered on tag creation (pattern: `v*`)
- Steps:
  1. Extracts version from tag
  2. Finds the successful CD run for the same commit and downloads the `chocolatey` artifact
  3. Validates `aws-vault.{version}.nupkg` is present in `artifacts/chocolatey/packages/`
  4. Publishes to Chocolatey using the downloaded package
  5. Checks for existing draft release and publishes it (or creates new release if missing)
  6. `dotnet cake --target clean` - Cleanup

**Required Secrets/Variables**:
- Organization variable: `CHOCOLATEY_SERVER` (e.g., `https://push.chocolatey.org/`)
- Repository secret: `CHOCOLATEY_API_KEY` (Chocolatey API key)
- Repository secret: `GH_PAT` (optional but recommended) - GitHub Personal Access Token with `repo` and `workflow` permissions
  - Enables auto-merge for update PRs
  - Allows workflows to trigger on PR events created by automation
  - Falls back to `github.token` if not configured (auto-merge disabled)

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

# Generate draft release notes (uses recent commits, no tags required)
dotnet cake --target GenerateDraftReleaseNotes --release-version 7.9.6 --release-previous-version 7.9.5

# Test release notes with custom author (defaults to aws-vault-chocolatey)
dotnet cake --target GenerateDraftReleaseNotes --release-version 7.9.6 --release-previous-version 7.9.5 --release-author "your-name"

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

## Documentation Maintenance

**Keep Instructions Current**: When making changes to the build system, workflows, or project structure, always update [.github/copilot-instructions.md](../.github/copilot-instructions.md) to reflect:
- New or removed Cake tasks
- Changes to workflow behavior or triggers
- Updates to build conventions or patterns
- New scripts or configuration files
- Modified dependencies or tool versions

**Documentation Scope**:
- Architecture and component overview
- Build task descriptions and usage examples
- Workflow orchestration and automation
- Local development setup and common operations
- Integration points and configuration requirements

**Update Checklist**:
- Remove references to deprecated tasks or scripts
- Add examples for new functionality
- Update file paths if files are moved or renamed
- Revise workflow descriptions when CI/CD changes
- Keep tool version numbers current
