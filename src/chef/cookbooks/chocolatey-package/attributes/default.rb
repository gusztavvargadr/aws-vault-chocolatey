default['chocolatey-package'] = {
  'id' => 'aws-vault',
  'title' => 'AWS Vault',
  'project-source-url' => 'https://github.com/ByteNess/aws-vault/',
  'project-version' => ENV['CHOCOLATEY_PROJECT_VERSION'],
  'package-source-url' => 'https://github.com/gusztavvargadr/aws-vault-chocolatey/',
  'package-version' => ENV['CHOCOLATEY_PACKAGE_VERSION'],
  'directory-path' => "#{ENV['ARTIFACTS_DIR']}/chocolatey/packages/",
}
