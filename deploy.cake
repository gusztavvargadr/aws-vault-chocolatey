#load ./build/cake/core.cake

Task("Restore")
  .IsDependentOn("RestoreCore")
  .Does(() => {
    if (chocolateySource == defaultChocolateySource) {
      EnsureDirectoryExists("./artifacts/chocolatey-server/packages/");

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
      DockerComposeRun(settings, service);
    }

    // var uninstallSettings = new ChocolateyUninstallSettings {
    // };
    // ChocolateyUninstall(packageName, uninstallSettings);

    // using(var process = StartAndReturnProcess(
    //   packageFilename,
    //   new ProcessSettings {
    //     Arguments = "--version",
    //     RedirectStandardOutput = true
    //   }
    // )) {
    //   process.WaitForExit();
    //   if (process.GetExitCode() != 0) {
    //     throw new Exception($"Error executing '{packageFilename}': '{process.GetExitCode()}'.");
    //   }

    //   var actualVersion = string.Join(Environment.NewLine, process.GetStandardOutput());
    //   Information($"Actual version: '{actualVersion}'.");
    //   var expectedVersion = $"v{appVersion}";
    //   if (actualVersion != expectedVersion) {
    //     throw new Exception($"Actual version '{actualVersion}' does not match expected version '{expectedVersion}'.");
    //   }
    // }
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
