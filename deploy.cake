#load "./build/core.cake"

Restored = () => {
  CopyFiles(artifactsDirectory.Path + $"/**/{packageName}.${packageVersion}.nupkg", workDirectory);

  var upSettings = new DockerComposeUpSettings {
    DetachedMode = true
  };
  var service = "registry";
  DockerComposeUp(upSettings, service);
};

Task("Build")
  .IsDependentOn("Restore")
  .Does(() => {
    var pushSettings = new ChocolateyPushSettings {
      Source = packageRegistryPush
    };
    ChocolateyPush(GetFiles(workDirectory.Path + $"/**/{packageName}.${packageVersion}.nupkg"), pushSettings);
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => {
    var installSettings = new ChocolateyInstallSettings {
      Source = packageRegistryPull,
      Version = packageVersion,
      Prerelease = !string.IsNullOrEmpty(sourceSemVer.Prerelease)
    };
    ChocolateyInstall(packageName, installSettings);

    var uninstallSettings = new ChocolateyUninstallSettings {
    };
    ChocolateyUninstall(packageName, uninstallSettings);
  });

Task("Package")
  .IsDependentOn("Test")
  .Does(() => {
  });

Task("Publish")
  .IsDependentOn("Package")
  .Does(() => {
  });

RunTarget(target);
