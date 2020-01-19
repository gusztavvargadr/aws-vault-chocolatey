choco install aws-vault --confirm --no-progress --version $($args[0]) --prerelease --source ./artifacts/chocolatey/packages/
choco uninstall aws-vault --confirm --version $($args[0])

exit $LASTEXITCODE
