# Agents

Read [README.md](README.md) and [CONTRIBUTING.md](CONTRIBUTING.md) before changing anything.

## Ask before

- `dotnet cake --target Clean`, because it prunes the machine's stopped containers, dangling images, and whole build cache, not only this repository's.
- `dotnet cake --target Publish`, or publishing a draft release, because both push to the public Chocolatey feed, which can't be undone. Publishing a draft is the maintainer's step.

Where Windows containers can't run, say which checks couldn't run, and don't report a pass.

## Conventions

- Run every command in PowerShell 7 (`pwsh`), not in a Bash shell such as Git Bash, even where a Bash tool is the default.
- Specs and plans go on the issue or the pull request, not in `docs/superpowers`.
- Review findings go on the pull request, as review comments on the lines they concern. Each thread gets a reply saying what was done: fixed (with the commit), moved to an issue (with the link), or left as is (with why). Then the thread is resolved.
- Findings outside the pull request's scope become issues.
- CI is the evidence.
