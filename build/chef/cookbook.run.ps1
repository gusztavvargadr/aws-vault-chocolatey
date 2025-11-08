pushd ./src/chef/
C:/opscode/chef/bin/chef-client.bat --config client.rb --minimal-ohai --no-fips --override-runlist "chocolatey-package::default"
popd

exit $LASTEXITCODE
