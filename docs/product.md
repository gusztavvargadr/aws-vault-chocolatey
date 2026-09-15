# Product

This repository is the source of the [`aws-vault` Chocolatey package](https://chocolatey.org/packages/aws-vault). It is for Windows users who install and update their tools with Chocolatey and want [AWS Vault](https://github.com/ByteNess/aws-vault) among them.

## Why

ByteNess builds AWS Vault for Windows and publishes the binary on GitHub releases. It does not publish it to Chocolatey.

So a Windows user who manages tools with Chocolatey downloads an `.exe` by hand, puts it on the path by hand, and finds out about each new version by hand. This repository removes all three. `choco install aws-vault` puts the tool on the path, and `choco upgrade aws-vault` moves it to the current release.

The work of doing that falls on one maintainer, not on the user. It has to stay small enough that one person keeps paying it for years, which is what shapes everything below.

## How

**Repackage, do not fork.** The package ships the upstream binary exactly as ByteNess published it. Nothing here compiles, patches, or configures AWS Vault, and a bug in the tool goes to ByteNess. This is the belief the rest follows from: a repackager with no opinions of its own is cheap to keep running for years.

**Automate up to the irreversible step.** A daily check finds a new upstream release, opens a pull request, and merges it once the package builds and installs cleanly. The maintainer publishes the draft release by hand. A push to the Chocolatey community feed is public and cannot be taken back, so a person stays in that one step on purpose.

**One version at a time, in order.** The update check takes the earliest upstream release this package does not have yet, not the newest. Five upstream releases become five package versions rather than one jump, so the Chocolatey version history has no gaps and a user on an older version still has a path forward.

**Ship the binary inside the package.** The package includes the `.exe` and the checksums for it, rather than downloading it while installing. A Chocolatey moderator can then check what they are approving, and an install does not depend on GitHub being up.

## What

Today this repository:

- Follows releases of [ByteNess/aws-vault](https://github.com/ByteNess/aws-vault), and opens a pull request for each one it does not have yet.
- Builds `aws-vault.<version>.nupkg` around the `windows-amd64` binary, with the checksums a moderator needs.
- Installs and uninstalls that package in a Windows container before anyone sees it.
- Publishes it to the Chocolatey community feed when the maintainer publishes the matching release.

Deliberately out:

- Any platform other than Windows. Chocolatey is a Windows package manager, and the upstream project already serves macOS and Linux through other package managers.
- Any package manager other than Chocolatey. A second one doubles the maintenance for the same binary.
- Anything about how AWS Vault behaves: no wrapper, no default profile, no configuration of `~/.aws/config`. That belongs upstream, or to the person using the tool.
- Any change to the binary. The package is a container for it and nothing more.

## Direction

This repository is working when a new upstream release becomes a published Chocolatey package with one deliberate action from the maintainer, and a user updating through Chocolatey never notices that the two projects are separate.

There is no end state. AWS Vault keeps releasing, and this keeps following. The sign it has stopped working is an upstream release that sits unpackaged for weeks, or a version that never gets published because its pull request went red and nobody noticed.

## Terms

- **Upstream** is [ByteNess/aws-vault](https://github.com/ByteNess/aws-vault), the project this repository packages. It is not the original `99designs/aws-vault`, which ByteNess took over.
- **Source version** is the upstream release being packaged, such as `7.14.0`. The submodule under `lib/` points at its tag.
- **Package version** is the version the Chocolatey package is published under. It matches the source version today, and it is a separate number because a fix to the packaging alone would need a new one while the source version stayed put.
