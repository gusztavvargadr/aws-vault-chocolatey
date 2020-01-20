default['chocolatey-package'] = {
  'id' => 'aws-vault',
  'title' => 'AWS Vault',
  'project-source-url' => 'https://github.com/99designs/aws-vault/',
  'project-version' => ENV['CHOCOLATEY_PROJECT_VERSION'],
  'package-source-url' => 'https://github.com/gusztavvargadr/aws-vault-chocolatey/',
  'package-version' => ENV['CHOCOLATEY_PACKAGE_VERSION'],
  'directory-path' => 'C:/opt/chef/artifacts/chocolatey/packages/',
}
