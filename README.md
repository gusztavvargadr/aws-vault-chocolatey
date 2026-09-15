# aws-vault-chocolatey

[![GitHub Actions build](https://github.com/gusztavvargadr/aws-vault-chocolatey/actions/workflows/cd.yml/badge.svg)](https://github.com/gusztavvargadr/aws-vault-chocolatey/actions/workflows/cd.yml)
[![GitHub latest release](https://img.shields.io/github/release/gusztavvargadr/aws-vault-chocolatey.svg)](https://github.com/gusztavvargadr/aws-vault-chocolatey/releases/latest)
[![Chocolatey downloads](https://img.shields.io/chocolatey/dt/aws-vault.svg)](https://chocolatey.org/packages/aws-vault)

This repository is the source of the [Chocolatey package] for [AWS Vault] by [ByteNess].

> AWS Vault is a tool to securely store and access AWS credentials in a development environment.
>
> AWS Vault stores IAM credentials in your operating system's secure keystore and then generates temporary credentials from those to expose to your shell and applications. It's designed to be complementary to the AWS CLI tools, and is aware of your profiles and configuration in ~/.aws/config.

For more about the tool itself, see [the original repository][AWS Vault].

## Installing

```powershell
choco install aws-vault
```

## Asking

The documents in this repository already answer the questions below, and `guides-g` is what finds the right one. So start a session here and ask, rather than reading a copy of the answer that can go stale:

```
Compose /guides-g and tell me: how does a new upstream release become a published package?
```

Swap in whichever question you have.

- What is this for, and what is deliberately not?
- What do I need on my machine to build the package?
- What do I run to build and test it?
- What has to pass before a change lands?

An agent starts at [AGENTS.md](AGENTS.md) instead, which names the document that answers each category.

[Chocolatey package]: https://chocolatey.org/packages/aws-vault
[AWS Vault]: https://github.com/ByteNess/aws-vault
[ByteNess]:  https://github.com/ByteNess
