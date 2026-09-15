# Engineering

The build is a Cake script in C# that calls PowerShell 7 scripts and a Windows container. There is no application code, no test framework, and no linter. The pipeline itself is the suite, which the [checks](#checks) below explain.

## Environment

The full pipeline needs Windows. The container that runs every `choco` command is a Windows Server Core image, and the version check runs the packaged `.exe` directly. A macOS or Linux machine can read the repository and run `dotnet cake --description`, and cannot run `Build` or anything after it.

A machine needs the .NET SDK, PowerShell 7, and Docker Desktop switched to Windows containers.

Each version this repository controls is pinned in one file:

| What | Where |
| --- | --- |
| Cake | `.config/dotnet-tools.json` |
| Chocolatey, and the Windows base image | `build/chocolatey/Dockerfile` |
| The upstream AWS Vault release | `build/chocolatey/package.json` |
| The GitHub Actions runner images | each file under `.github/workflows/` |

The .NET SDK version is not pinned, because nothing here compiles against a framework. Cake is restored as a local tool, so the pinned version is what runs.

## Operations

`dotnet cake` is the one entry point. Restore the local tool once first:

```sh
dotnet tool restore
```

`build.cake` is the definition, and it describes itself. Ask it rather than reading a copy of the list:

```sh
dotnet cake --description   # every target and what it does
dotnet cake --tree          # the dependency graph
```

Targets depend in one line, so naming a later one runs every earlier one. The default is `Package`, which is the full local check.

Two arguments matter when working on the packaging rather than on a version bump. `--source-version` packages an upstream release other than the one in `package.json`, and `--package-version` sets the version the package is published under.

```sh
dotnet cake --target Package --source-version 7.13.0
```

## Checks

There is no unit test suite to run red then green against. What stands in for one is the pipeline: it downloads the real binary, compares the version it reports against the version the package claims, packs it, and installs and uninstalls it in a clean container. A change that breaks any of that fails the target.

The prediction still MUST come first. State what will fail and why before running the target, exactly as the engineering guide requires, because that is what separates a check from a command that happened to exit non-zero.

Which change requires which check:

| Change | Check |
| --- | --- |
| The version in `package.json` | `dotnet cake --target Package`, or the pull request, which runs the same thing. |
| `New-ChocolateyPackage.ps1`, or anything about the package contents | `dotnet cake --target Package`, then read the generated `artifacts/chocolatey/packages/aws-vault/aws-vault.nuspec` and `tools/VERIFICATION.txt`. |
| `build.cake` | `dotnet cake --description` and `dotnet cake --tree` first, because a broken dependency shows there without running anything, then `--target Package`. |
| The `Dockerfile` | `dotnet cake --target Restore` to rebuild the image, then `--target Package`. |
| `New-ReleaseNotes.ps1`, `Get-NextVersion.ps1`, or the release template | Run the script directly with arguments that reproduce the case, and read what it wrote. None of them is in the `Package` line. |
| A workflow | Nothing local runs it. Push the branch and read the run. |
| Documentation only | Read the changed text and every document that links to it. No command. |

Where a correction needs a failing check the pipeline does not give, make one with a focused probe and remove it once it has proved its point. Packaging a version that does not exist upstream, or passing a `--package-version` that disagrees with `--source-version`, both produce a predictable failure to start from.

Every pull request runs Continuous Delivery, which runs `dotnet cake --target package` on Windows. A change is not proven until that is green.

## Escalation

An agent MUST ask the maintainer before each of these, and MUST NOT treat any of them as an ordinary step.

- **`dotnet cake --target Clean`.** It runs `docker container prune`, `docker image prune`, and `docker builder prune -af`, which delete containers, images, and the entire build cache on the machine, including everything unrelated to this repository.
- **`dotnet cake --target Publish`, and publishing a draft GitHub release.** Both push the package to the Chocolatey community feed, which is public and cannot be taken back. Publishing the draft release is the maintainer's step by design, and an agent MUST leave it alone.

An agent on a machine that cannot run Windows containers MUST report that the check could not run here. It MUST NOT report a pass on the strength of the targets that did run.

## PowerShell conventions

Every script under `build/` that does real work opens with comment-based help, then declared parameters, then strict mode. A new one MUST do the same:

```powershell
<#
.SYNOPSIS
One line saying what the script does.
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ConfigPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
```

The scripts under `build/chocolatey/` named `package.*.ps1` are the exception. Each one is a `choco` command or two followed by `exit $LASTEXITCODE`, and it stays that thin so the container runs Chocolatey and nothing else.

A script that takes an output path MUST write its result there, and MUST NOT decide that path itself, so the call in `build.cake` stays the one place paths are set.

## Cake conventions

Every task MUST have a `.Description()`, because `dotnet cake --description` is how a reader finds out what exists:

```csharp
Task("Package")
  .Description("Pack the package and prove it installs and uninstalls in a clean container.")
  .IsDependentOn("Build")
```

Every `docker` call MUST go through `RunDockerCommand()`, which logs the command, captures both streams, and throws on a non-zero exit. A `StartProcess` call for anything else MUST check the exit code and throw, rather than letting the target report success.
