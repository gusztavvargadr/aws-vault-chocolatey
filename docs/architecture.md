# Architecture

This repository is a build pipeline and nothing else. It turns one input, the upstream Windows binary, into one output, `aws-vault.<version>.nupkg`. No code here ships to anybody.

## Upstream tracking

Two things record which upstream release is being packaged.

- `build/chocolatey/package.json` states the version. It is the only place the build reads it from.
- `lib/ByteNess/aws-vault` is a git submodule pointed at the matching upstream tag, so the exact source and history behind a package version is available to read without leaving the repository.

The daily update check moves both together. Nothing else writes either of them.

## Build definition

`build.cake` is the one definition of what can be run. Its targets depend in a single line, so asking for a later one runs every earlier one: `Init`, then `Restore`, then `Build`, then `Package`, then `Publish`. `Clean` and `GenerateDraftReleaseNotes` sit outside that line.

The targets do no work themselves. Each one calls out to a PowerShell script under `build/`, or to `docker compose`. Nothing calls back: no script and no Dockerfile invokes Cake.

## Native and containerized halves

The pipeline splits at the point where a real Chocolatey install is needed.

Everything up to a finished package directory runs natively, on PowerShell 7: downloading the binary, computing the MD5, SHA256, and SHA512 checksums, and generating the `.nuspec`, `VERIFICATION.txt`, and `LICENSE.txt`. None of that needs Windows.

Everything that runs `choco` runs inside a Windows Server Core container: `choco pack`, then `choco install` and `choco uninstall` against the local package directory, then `choco push`. A container is used because an install test is only worth something on a machine that starts clean, and because the host is not required to have Chocolatey on it.

The two halves exchange one thing. `docker-compose.yml` mounts the repository at `C:/opt/docker/work/`, so both of them read and write the same `artifacts/` directory.

## Delivery pipeline

Three GitHub Actions workflows run the repository, and they hand work to each other through artifacts and tags rather than calling each other.

- **Check for Updates** runs daily on Linux. It asks the GitHub API for upstream releases, takes the earliest one this package does not have, points the submodule at that tag, writes the version into `package.json`, opens a pull request, and turns on auto-merge.
- **Continuous Delivery** runs on Windows for every pull request and every push to `main`. It runs `dotnet cake --target package` and uploads the result as the `chocolatey` artifact. On `main`, it creates or updates a draft GitHub release from generated notes when the package version has no published release, and creates no tag.
- **Release** runs on Windows when a `v*` tag appears, which happens when the maintainer publishes that draft. It finds the successful Continuous Delivery run for the same commit, downloads its `chocolatey` artifact, and pushes that file to the Chocolatey feed.

The tag is the handoff between the second and the third. Publishing the draft release is the only manual step, and it is what makes the package public.

## Invariants

A change is checked against each of these.

- The `.exe` inside the package MUST be the file ByteNess published, unmodified. Nothing in this repository compiles, patches, or rewrites AWS Vault.
- `build/chocolatey/package.json` MUST be the only place that states which upstream version is packaged. No script and no workflow hardcodes a version.
- A package MUST NOT be produced whose binary reports a version different from the version the package claims.
- A package version MUST be pushed to the Chocolatey feed only as the exact file a successful Continuous Delivery run already built and installed. Nothing rebuilds it at release time.
- Every `choco` command MUST run inside the container. The host is never assumed to have Chocolatey on it.
- A workflow MUST NOT contain build logic of its own. It calls a `build.cake` target or a script under `build/`, so the same step can run on a maintainer's machine.
- The Chocolatey server and API key MUST be read from the environment at the point of use. Neither is written into a file in this repository.
