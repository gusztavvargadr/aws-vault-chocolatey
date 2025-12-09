var target = Argument("target", "Package");
var packageName = "aws-vault";

var sourceVersion = Argument("source-version", "7.8.2");
var buildVersion = Argument("build-version", $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
var projectVersion = Argument("project-version", sourceVersion);
var packageVersion = Argument("package-version", sourceVersion);

var chocolateyServer = EnvironmentVariable("CHOCOLATEY_SERVER", string.Empty);

void RunDockerCommand(string arguments) {
  Information($"Running docker command: docker {arguments}");
  using(var process = StartAndReturnProcess(
    "docker",
    new ProcessSettings {
      Arguments = arguments,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
    }
  )) {
    process.WaitForExit();
    var output = string.Join(Environment.NewLine, process.GetStandardOutput());
    var error = string.Join(Environment.NewLine, process.GetStandardError());
    
    if (!string.IsNullOrWhiteSpace(output)) {
      Information(output);
    }
    if (!string.IsNullOrWhiteSpace(error)) {
      Information(error);
    }
    
    if (process.GetExitCode() != 0) {
      throw new Exception($"Docker command failed with exit code '{process.GetExitCode()}': docker {arguments}");
    }
  }
}

Task("Init")
  .Does(() => {
    Information($"Source version: '{sourceVersion}'.");
    Information($"Build version: '{buildVersion}'.");
    Information($"Project version: '{projectVersion}'.");
    Information($"Package version: '{packageVersion}'.");

    RunDockerCommand("--version");
    RunDockerCommand("compose version");

    RunDockerCommand("system df");
    RunDockerCommand("container ls -a");
    RunDockerCommand("image ls -a");
  });

Task("Restore")
  .IsDependentOn("Init")
  .Does(() => {
    // No longer need Chef installation
    // Build docker image for testing/publishing (still useful for isolation)
    RunDockerCommand("compose build chocolatey");
  });

Task("Build")
  .IsDependentOn("Restore")
  .Does(() => {
    // Set environment variables for PowerShell script
    Environment.SetEnvironmentVariable("CHOCOLATEY_PROJECT_VERSION", projectVersion);
    Environment.SetEnvironmentVariable("CHOCOLATEY_PACKAGE_VERSION", packageVersion);

    // Run PowerShell package builder
    var artifactsDir = MakeAbsolute(Directory("./artifacts/")).FullPath;
    var configPath = "./build/chocolatey/package.json";
    var result = StartProcess("pwsh", new ProcessSettings {
      Arguments = $"-File ./build/chocolatey/New-ChocolateyPackage.ps1 -ConfigPath {configPath} -OutputDirectory \"{artifactsDir}/chocolatey/packages/\""
    });
    
    if (result != 0) {
      throw new Exception($"Package build failed with exit code: {result}");
    }

    // Verify the downloaded binary version
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
  .IsDependentOn("Build")
  .Does(() => {
    RunDockerCommand($"compose run --rm --entrypoint \"powershell -File ./build/chocolatey/package.pack.ps1\" chocolatey");

    RunDockerCommand($"compose run --rm --entrypoint \"powershell -File ./build/chocolatey/package.install.ps1\" chocolatey {packageVersion}");
  });

Task("Publish")
  .IsDependentOn("Package")
  .Does(() => {
    if (string.IsNullOrEmpty(chocolateyServer)) {
      Warning("Chocolatey server is not configured, skipping publish.");
      return;
    }

    RunDockerCommand($"compose run --rm --entrypoint \"powershell -File ./build/chocolatey/package.push.ps1\" chocolatey {packageVersion}");
  });

Task("Clean")
  .IsDependentOn("Init")
  .Does(() => {
    RunDockerCommand("container prune -f");

    RunDockerCommand("compose down --rmi local --volumes");

    RunDockerCommand("image prune -f");
    RunDockerCommand("builder prune -af");

    CleanDirectory("./artifacts/");
  });

RunTarget(target);
