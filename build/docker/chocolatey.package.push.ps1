choco push ./artifacts/chocolatey/packages/aws-vault.$($args[0]).nupkg --source $env:CHOCOLATEY_SOURCE --api-key $env:CHOCOLATEY_API_KEY --force
