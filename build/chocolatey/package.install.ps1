choco install aws-vault --confirm --no-progress --version $($args[0]) --prerelease --source ./artifacts/chocolatey/packages/ --verbose --debug
choco uninstall aws-vault --confirm --version $($args[0]) --verbose --debug

exit $LASTEXITCODE
