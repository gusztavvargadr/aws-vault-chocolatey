chef install ./src/chef/cookbooks/chocolatey-package/Policyfile.rb
chef export ./src/chef/cookbooks/chocolatey-package/Policyfile.rb ./.chef/policies/chocolatey-package/ --force

pushd ./.chef/policies/chocolatey-package/
chef-client --local-mode
popd

choco pack ./.chocolatey/packages/aws-vault/aws-vault.nuspec --output-directory ./.chocolatey/packages/
