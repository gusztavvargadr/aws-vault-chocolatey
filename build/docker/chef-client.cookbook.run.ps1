pushd ./src/chef/
chef-client --local-mode --minimal-ohai --override-runlist "chocolatey-package::default"
popd

exit $LASTEXITCODE
