#load ./build/cake/core.cake

Task("Restore")
  .IsDependentOn("Version")
  .Does(() => {
    {
      var settings = new DockerComposeBuildSettings {
      };
      var services = new [] { "chocolatey", "chef" };
      DockerComposeBuild(settings, services);
    }
  });

Task("Build")
  .IsDependentOn("Restore")
  .Does(() => {
    {
      var settings = new DockerComposeRunSettings {
        Entrypoint = "powershell"
      };
      var service = "chef";
      var command = "-File ./build/docker/chef.policy.ps1";
      DockerComposeRun(settings, service, command);
    }

    {
      var settings = new DockerComposeRunSettings {
      };
      var service = "chocolatey";
      var command = "pack ./.chocolatey/packages/aws-vault/aws-vault.nuspec --output-directory ./.chocolatey/packages/";
      DockerComposeRun(settings, service, command);
    }
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => {
//     var installSettings = new ChocolateyInstallSettings {
//       // Debug = true,
//       // Verbose = true,
//       WorkingDirectory = workDirectory,
//       Source = ".",
//       Version = packageVersion,
//       Prerelease = !string.IsNullOrEmpty(sourceSemVer.Prerelease)
//     };
//     ChocolateyInstall(packageName, installSettings);

//     using(var process = StartAndReturnProcess(
//       packageFilename,
//       new ProcessSettings {
//         Arguments = "--version",
//         RedirectStandardOutput = true
//       }
//     )) {
//       process.WaitForExit();
//       if (process.GetExitCode() != 0) {
//         throw new Exception($"Error executing '{packageFilename}': '{process.GetExitCode()}'.");
//       }

//       var actualVersion = string.Join(Environment.NewLine, process.GetStandardOutput());
//       Information($"Actual version: '{actualVersion}'.");
//       var expectedVersion = $"v{appVersion}";
//       if (actualVersion != expectedVersion) {
//         throw new Exception($"Actual version '{actualVersion}' does not match expected version '{expectedVersion}'.");
//       }
//     }

//     var uninstallSettings = new ChocolateyUninstallSettings {
//     };
//     ChocolateyUninstall(packageName, uninstallSettings);
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
