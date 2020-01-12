chef install ./src/chef/cookbooks/chocolatey-package/Policyfile.rb
chef export ./src/chef/cookbooks/chocolatey-package/Policyfile.lock.json ./.chef/policies/chocolatey-package --force

pushd ./.chef/policies/chocolatey-package
chef-client --local-mode
popd
