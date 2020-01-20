#load ./build/cake/core.cake

Task("Restore")
  .IsDependentOn("RestoreCore")
  .Does(() => {
    if (packageServer == defaultChocolateyServer) {
      var settings = new DockerComposeUpSettings {
        DetachedMode = true
      };
      var services = new [] { "chocolatey-server" };
      DockerComposeUp(settings, services);
    }
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
    var settings = new DockerComposeRunSettings {
      Entrypoint = "powershell -File ./build/docker/chocolatey.package.push.ps1",
    };
    var service = "chocolatey";
    var command = $"{packageVersion}";
    DockerComposeRun(settings, service, command);
  });

RunTarget(target);
