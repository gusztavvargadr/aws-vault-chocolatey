directory_path = node['chocolatey-package']['directory-path']
id = node['chocolatey-package']['id']
title = node['chocolatey-package']['title']
package_source_url = node['chocolatey-package']['package-source-url']
package_version = node['chocolatey-package']['package-version']
project_source_url = node['chocolatey-package']['project-source-url']
project_version = node['chocolatey-package']['project-version']

project_download_url = "#{project_source_url}releases/download/v#{project_version}/aws-vault-windows-386.exe"
project_download_path = "#{directory_path}tools/aws-vault.exe"
project_download_hash_md5 = ''
project_download_hash_sha256 = ''
project_download_hash_sha512 = ''

project_license_url = "#{project_source_url}blob/v#{project_version}/LICENSE"

directory directory_path do
  recursive true
  action :create
end

template "#{directory_path}aws-vault.nuspec" do
  source 'aws-vault.nuspec.erb'
  variables(
    id: id,
    title: title,
    package_source_url: package_source_url,
    package_version: package_version,
    project_source_url: project_source_url,
    project_version: project_version,
    project_license_url: project_license_url
  )
  action :create
end

directory "#{directory_path}tools" do
  recursive true
  action :create
end

remote_file project_download_path do
  source project_download_url
  action :create
end

ruby_block 'project_download_hash' do
  block do
    project_download_hash_md5 = Digest::MD5.file(project_download_path).hexdigest.upcase
    project_download_hash_sha256 = Digest::SHA256.file(project_download_path).hexdigest.upcase
    project_download_hash_sha512 = Digest::SHA512.file(project_download_path).hexdigest.upcase
  end
  action :run
end

template "#{directory_path}tools/VERIFICATION.txt" do
  source 'tools.VERIFICATION.txt.erb'
  variables(
    lazy {
      {
        project_download_url: project_download_url,
        project_download_hash_md5: project_download_hash_md5,
        project_download_hash_sha256: project_download_hash_sha256,
        project_download_hash_sha512: project_download_hash_sha512,
      }
    }
  )
  action :create
end

template "#{directory_path}tools/LICENSE.txt" do
  source 'tools.LICENSE.txt.erb'
  variables(
    project_license_url: project_license_url
  )
  action :create
end
