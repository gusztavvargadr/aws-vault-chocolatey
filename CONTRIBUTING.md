# Contributing

## Prerequisites

- Windows, with Docker switched to Windows containers.
- The .NET SDK, to run [Cake](https://cakebuild.net).
- PowerShell 7.

Elsewhere, `Restore` and every target after it can't run.

## Build

```sh
dotnet tool restore
dotnet cake [--target <target>]
```

- `--description` lists the targets, and `--tree` shows what each one runs first.
- `Package`, the default, downloads the binary, checks the version it reports against the upstream version, packs it, and installs and uninstalls it in a clean container.
- `Publish` pushes the package `Package` left under `artifacts/`, without rebuilding it, and fails when there is none or `CHOCOLATEY_SERVER` isn't set.
- `--source-version` packages another upstream release than the one in `build/chocolatey/package.json`, and `--package-version` sets the version the package is published under.

## Checks

- The version, the package contents, `build.cake`, or the `Dockerfile`: `Package`. For the contents, also read the generated `.nuspec` and `tools/VERIFICATION.txt` under `artifacts/`. For `build.cake`, run `--tree` first. For the `Dockerfile`, `Restore` rebuilds the image.
- `New-ReleaseNotes.ps1`, `Get-NextVersion.ps1`, `Update-SourceVersion.ps1`, or the release template: run the script directly, and read what it wrote. `New-ReleaseNotes.ps1` needs `gh` signed in, or `GH_TOKEN`. `Update-SourceVersion.ps1` moves the submodule and `package.json`, so put both back afterwards.
- A workflow: push the branch, and read the run.

Each pull request runs `Package` in Continuous Delivery, and isn't done until it passes.

## Rules

- The packaged `.exe` is the file ByteNess published, unmodified.
- `build/chocolatey/package.json` is the only place that states the upstream version, and only Check for Updates changes it.
- A package whose binary reports another version than the upstream version is never produced.
- The feed only gets the exact file a passing Continuous Delivery run built. Nothing rebuilds it at release.
- Every `choco` command runs in the container.
- A workflow has no build logic of its own. It calls a `build.cake` target or a script under `build/`.
- The Chocolatey server and API key come from the environment.

## Conventions

- A script under `build/` opens with comment-based help, then `param()`, then `Set-StrictMode -Version Latest` and `$ErrorActionPreference = 'Stop'`. The thin `package.*.ps1` scripts are the exception. A script that takes an output path writes there.
- Every Cake task has a `.Description()`. Every `docker` call goes through `RunDockerCommand()`, and any other `StartProcess` checks its exit code.

## Pipelines

- Check for Updates runs daily. It opens a pull request that moves `lib/ByteNess/aws-vault` and `package.json` to the earliest upstream release not packaged yet, and only once the current version's release is published. It opens it with the `BUMP_TOKEN` secret, because a pull request the workflow token opens starts no workflows, and Continuous Delivery has to run on it.
- Merging that pull request runs Continuous Delivery on `main`, which creates the draft release.
- Publishing the draft creates the `v*` tag, and Release pushes that Continuous Delivery run's package to the feed.
