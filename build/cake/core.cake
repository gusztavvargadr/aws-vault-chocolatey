var target = Argument("target", "Publish");
var packageName = "aws-vault";

var sourceVersion = Argument("source-version", "7.6.5");
var buildVersion = Argument("build-version", string.Empty);
var projectVersion = Argument("project-version", string.Empty);
var packageVersion = Argument("package-version", string.Empty);

var chocolateyServer = EnvironmentVariable("CHOCOLATEY_SERVER", string.Empty);

Task("Init")
  .Does(() => {
    StartProcess("docker", "--version");
    StartProcess("docker", "compose version");

    StartProcess("docker", "system df");
    StartProcess("docker", "container ls -a");
    StartProcess("docker", "image ls -a");
  });

Task("Restore")
  .IsDependentOn("Init")
  .Does(() => {
    StartProcess("docker", "compose build chef-client chocolatey");
  });

Task("Version")
  .IsDependentOn("Restore")
  .Does((context) => {
    Information($"Source version: '{sourceVersion}'.");

    if (string.IsNullOrEmpty(buildVersion)) {
      buildVersion = $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    }
    Information($"Build version: '{buildVersion}'.");

    if (string.IsNullOrEmpty(projectVersion))
    {
      projectVersion = sourceVersion;
    }
    Information($"Project version: '{projectVersion}'.");

    if (string.IsNullOrEmpty(packageVersion)) {
      packageVersion = sourceVersion;
    }
    Information($"Package version: '{packageVersion}'.");
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
