#load ./build/cake/core.cake

Task("Build")
  .IsDependentOn("Version")
  .Does(() => {
    var settings = new DockerComposeRunSettings {
      Entrypoint = "powershell -File ./build/docker/chef-client.cookbook.run.ps1",
      Environment = new [] {
        $"CHOCOLATEY_PROJECT_VERSION={projectVersion}",
        $"CHOCOLATEY_PACKAGE_VERSION={packageVersion}"
      }
    };
    var service = "chef-client";
    DockerComposeRun(settings, service);
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => {
    var executablePath = "./artifacts/chocolatey/packages/aws-vault/tools/aws-vault.exe";
    using(var process = StartAndReturnProcess(
      executablePath,
      new ProcessSettings {
        Arguments = "--version",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
      }
    )) {
      process.WaitForExit();
      if (process.GetExitCode() != 0) {
        throw new Exception($"Error executing '{executablePath}': '{process.GetExitCode()}'.");
      }

      var actualVersion = string.Join(Environment.NewLine, process.GetStandardOutput().Concat(process.GetStandardError())).Trim();
      Information($"Actual version: '{actualVersion}'.");
      var expectedVersion = $"v{projectVersion}";
      Information($"Expected version: '{expectedVersion}'.");
      if (actualVersion != expectedVersion) {
        throw new Exception($"Actual version '{actualVersion}' does not match expected version '{expectedVersion}'.");
      }
    }
  });

Task("Package")
  .IsDependentOn("Test")
  .Does(() => {
    var settings = new DockerComposeRunSettings {
      Entrypoint = "powershell -File ./build/docker/chocolatey.package.pack.ps1",
    };
    var service = "chocolatey";
    DockerComposeRun(settings, service);
  });

Task("Publish")
  .IsDependentOn("Package")
  .Does(() => {
  });

RunTarget(target);
