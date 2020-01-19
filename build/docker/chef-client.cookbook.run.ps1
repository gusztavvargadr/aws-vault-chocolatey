pushd ./src/chef/
chef-client --local-mode --override-runlist "chocolatey-package::default" --minimal-ohai
popd

exit $LASTEXITCODE
