#load ./build/cake/core.cake

Task("Build")
  .IsDependentOn("Restore")
  .Does(() => {
    var pushSettings = new ChocolateyPushSettings {
      Source = packageRegistryPush
    };
    ChocolateyPush(GetFiles(workDirectory.Path + $"/**/{packageName}.{packageVersion}.nupkg"), pushSettings);
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => {
    var installSettings = new ChocolateyInstallSettings {
      // Debug = true,
      // Verbose = true,
      WorkingDirectory = workDirectory,
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
