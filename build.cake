var target = Argument("target", "Package");
var packageName = "aws-vault";

var sourceVersion = Argument("source-version", "7.7.9");
var buildVersion = Argument("build-version", $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
var projectVersion = Argument("project-version", sourceVersion);
var packageVersion = Argument("package-version", sourceVersion);

var chocolateyServer = EnvironmentVariable("CHOCOLATEY_SERVER", string.Empty);

Task("Init")
  .Does(() => {
    Information($"Source version: '{sourceVersion}'.");
    Information($"Build version: '{buildVersion}'.");
    Information($"Project version: '{projectVersion}'.");
    Information($"Package version: '{packageVersion}'.");

    StartProcess("choco", "--version");

    StartProcess("docker", "--version");
    StartProcess("docker", "compose version");

    StartProcess("docker", "system df");
    StartProcess("docker", "container ls -a");
    StartProcess("docker", "image ls -a");
  });

Task("Restore")
  .IsDependentOn("Init")
  .Does(() => {
    StartProcess("choco", "install -y chef-client --version 18.8.54 --no-progress");
    Environment.SetEnvironmentVariable("CHEF_LICENSE", "accept-silent");

    StartProcess("docker", "compose build chocolatey");
  });

Task("Build")
  .IsDependentOn("Restore")
  .Does(() => {
    Environment.SetEnvironmentVariable("CHOCOLATEY_PROJECT_VERSION", projectVersion);
    Environment.SetEnvironmentVariable("CHOCOLATEY_PACKAGE_VERSION", packageVersion);
    Environment.SetEnvironmentVariable("ARTIFACTS_DIR", MakeAbsolute(Directory("./artifacts/")).FullPath);

    StartProcess("powershell", "-File ./build/chef/cookbook.run.ps1");

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
    StartProcess("docker", $"compose run --rm --entrypoint \"powershell -File ./build/chocolatey/package.pack.ps1\" chocolatey");

    StartProcess("docker", $"compose run --rm --entrypoint \"powershell -File ./build/chocolatey/package.install.ps1\" chocolatey {packageVersion}");
  });

Task("Publish")
  .IsDependentOn("Package")
  .Does(() => {
    if (string.IsNullOrEmpty(chocolateyServer)) {
      Warning("Chocolatey server is not configured, skipping publish.");
      return;
    }

    StartProcess("docker", $"compose run --rm --entrypoint \"powershell -File ./build/chocolatey/package.push.ps1\" chocolatey {packageVersion}");
  });

Task("Clean")
  .IsDependentOn("Init")
  .Does(() => {
    StartProcess("docker", "container prune -f");

    StartProcess("docker", "compose down --rmi local --volumes");

    StartProcess("docker", "image prune -f");
    StartProcess("docker", "builder prune -af");

    CleanDirectory("./artifacts/");
  });

RunTarget(target);
