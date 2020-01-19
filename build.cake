#load ./build/cake/core.cake

Task("Restore")
  .IsDependentOn("RestoreCore")
  .Does(() => {
    var settings = new DockerComposeBuildSettings {
    };
    var services = new [] { "chef" };
    DockerComposeBuild(settings, services);
  });

Task("Build")
  .IsDependentOn("Restore")
  .Does(() => {
    {
      var settings = new DockerComposeRunSettings {
        Entrypoint = "powershell -File ./build/docker/chef.policy.run.ps1",
        Environment = new [] {
          $"CHOCOLATEY_PROJECT_VERSION={projectVersion}",
          $"CHOCOLATEY_PACKAGE_VERSION={packageVersion}"
        }
      };
      var service = "chef";
      DockerComposeRun(settings, service);
    }

    {
      var settings = new DockerComposeRunSettings {
        Entrypoint = "powershell -File ./build/docker/chocolatey.package.pack.ps1",
      };
      var service = "chocolatey";
      DockerComposeRun(settings, service);
    }
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => {
    var settings = new DockerComposeRunSettings {
      Entrypoint = "powershell -File ./build/docker/chocolatey.package.install.ps1",
    };
    var service = "chocolatey";
    var command = $"{packageVersion}";
    DockerComposeRun(settings, service, command);
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
