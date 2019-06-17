#load "core.cake"

Restored = () => {
  CopyFiles(artifactsDirectory.Path + "/**/*.nupkg", workDirectory);

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
      Source = "http://localhost:5000/chocolatey",
      ApiKey = "chocolateyrocks"
    };

    ChocolateyPush(GetFiles(workDirectory.Path + "/**/*.nupkg"), pushSettings);
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => {
    var installSettings = new ChocolateyInstallSettings {
      Source = "http://localhost:5000/chocolatey",
      Version = packageVersion
    };

    ChocolateyInstall(packageId, installSettings);

    var uninstallSettings = new ChocolateyUninstallSettings {
    };
    ChocolateyUninstall(packageId, uninstallSettings);
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
