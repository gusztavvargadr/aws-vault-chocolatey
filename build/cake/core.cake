#addin nuget:?package=Cake.Docker&version=0.11.0
#addin nuget:?package=Cake.SemVer&version=4.0.0
#addin nuget:?package=semver&version=2.0.4

var target = Argument("target", "Publish");
var packageName = "aws-vault";

var sourceVersion = Argument("source-version", string.Empty);
Semver.SemVersion sourceSemVer;
var buildVersion = Argument("build-version", string.Empty);
var projectVersion = Argument("project-version", string.Empty);
var packageVersion = Argument("package-version", string.Empty);

var defaultChocolateyServer = "http://chocolatey-server/chocolatey";
var chocolateyServer = EnvironmentVariable("CHOCOLATEY_SERVER", defaultChocolateyServer);

var packageServer = Argument("package-server", string.Empty);
if (string.IsNullOrEmpty(packageServer)) {
  packageServer = chocolateyServer;
}

Task("Init")
  .Does(() => {
    StartProcess("docker", "version");
    StartProcess("docker-compose", "version");
  });

Task("Version")
  .IsDependentOn("Init")
  .Does((context) => {
    if (string.IsNullOrEmpty(sourceVersion)) {
      {
        var settings = new DockerComposeUpSettings {
        };
        var services = new [] { "gitversion" };
        DockerComposeUp(settings, services);
      }

      {
        var runner = new GenericDockerComposeRunner<DockerComposeLogsSettings>(
          context.FileSystem,
          context.Environment,
          context.ProcessRunner,
          context.Tools
        );
        var settings = new DockerComposeLogsSettings {
          NoColor = true
        };
        var service = "gitversion";
        var output = runner.RunWithResult(
          "logs",
          settings,
          (items) => items.Where(item => item.Contains('|')).ToArray(),
          service
        ).Last();

        sourceVersion = output.Split('|')[1].Trim();
      }
    }
    Information($"Source version: '{sourceVersion}'.");
    sourceSemVer = ParseSemVer(sourceVersion);

    if (string.IsNullOrEmpty(buildVersion)) {
      buildVersion = $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    }
    Information($"Build version: '{buildVersion}'.");

    if (string.IsNullOrEmpty(projectVersion)) {
      projectVersion = new Semver.SemVersion(sourceSemVer.Major, sourceSemVer.Minor, sourceSemVer.Patch).ToString();
    }
    Information($"Project version: '{projectVersion}'.");

    if (string.IsNullOrEmpty(packageVersion)) {
      packageVersion = sourceVersion;
    }
    Information($"Package version: '{packageVersion}'.");
  });

Task("RestoreCore")
  .IsDependentOn("Version")
  .Does(() => {
    var settings = new DockerComposeBuildSettings {
    };
    var services = new [] { "chocolatey" };
    DockerComposeBuild(settings, services);
  });

Task("Clean")
  .IsDependentOn("Version")
  .Does(() => {
    var settings = new DockerComposeDownSettings {
      Rmi = "all"
    };
    DockerComposeDown(settings);
  });
