pushd ./src/chef/
C:/opscode/chef/bin/chef-client.bat --minimal-ohai --override-runlist "chocolatey-package::default"
popd

exit $LASTEXITCODE
