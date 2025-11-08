pushd ./src/chef/
C:/opscode/chef/bin/chef-client.bat --local-mode --minimal-ohai --override-runlist "chocolatey-package::default"
popd

exit $LASTEXITCODE
