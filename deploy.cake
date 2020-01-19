#load ./build/cake/core.cake

Task("Restore")
  .IsDependentOn("RestoreCore")
  .Does(() => {
  });

Task("Build")
  .IsDependentOn("Restore")
  .Does(() => {
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => {
    {
      var settings = new DockerComposeRunSettings {
        Entrypoint = "powershell -File ./build/docker/chocolatey.package.install.ps1",
      };
      var service = "chocolatey";
      var command = $"{packageVersion}";
      DockerComposeRun(settings, service, command);
    }
  });

Task("Package")
  .IsDependentOn("Test")
  .Does(() => {
  });

Task("Publish")
  .IsDependentOn("Package")
  .Does(() => {
    if (string.IsNullOrEmpty(packageRegistry)) {
      return;
    }

    var settings = new DockerComposeRunSettings {
      Entrypoint = "powershell -File ./build/docker/chocolatey.package.push.ps1",
    };
    var service = "chocolatey";
    var command = $"{packageVersion}";
    DockerComposeRun(settings, service, command);
  });

RunTarget(target);
