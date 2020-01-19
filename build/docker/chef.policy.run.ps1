chef install ./src/chef/cookbooks/chocolatey-package/Policyfile.rb
chef export ./src/chef/cookbooks/chocolatey-package/Policyfile.rb ./artifacts/chef/policies/chocolatey-package/ --force

pushd ./artifacts/chef/policies/chocolatey-package/
chef-client --local-mode
popd
