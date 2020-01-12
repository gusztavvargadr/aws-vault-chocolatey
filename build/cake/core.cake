#addin nuget:?package=Cake.Docker&version=0.11.0
#addin nuget:?package=Cake.SemVer&version=4.0.0
#addin nuget:?package=semver&version=2.0.4

var defaulTarget = "Publish";

var defaultPackageVersion = string.Empty;
var defaultProjectVersion = string.Empty;

var target = Argument("target", defaulTarget);

var packageVersion = Argument("package-version", defaultPackageVersion);
var projectVersion = Argument("project-version", defaultProjectVersion);

Semver.SemVersion packageSemVer;

Task("Init")
  .Does(() => {
    StartProcess("docker", "version");
    StartProcess("docker-compose", "version");

    {
      var settings = new DockerComposeBuildSettings {
      };
      var services = new [] { "gitversion" };
      DockerComposeBuild(settings, services);
    }
  });

Task("Version")
  .IsDependentOn("Init")
  .Does((context) => {
    if (packageVersion == defaultPackageVersion) {
      var upSettings = new DockerComposeUpSettings {
      };
      var upServices = new [] { "gitversion" };
      DockerComposeUp(upSettings, upServices);

      var logsRunner = new GenericDockerComposeRunner<DockerComposeLogsSettings>(
        context.FileSystem,
        context.Environment,
        context.ProcessRunner,
        context.Tools
      );
      var logsSettings = new DockerComposeLogsSettings {
        NoColor = true
      };
      var logsService = "gitversion";
      var logsOutput = logsRunner.RunWithResult(
        "logs",
        logsSettings,
        (items) => items.Where(item => item.Contains('|')).ToArray(),
        logsService
      ).Last();

      packageVersion = logsOutput.Split('|')[1].Trim();
    }
    packageSemVer = ParseSemVer(packageVersion);
    Information($"Package version: '{packageVersion}'.");

    if (projectVersion == defaultProjectVersion) {
      projectVersion = new Semver.SemVersion(packageSemVer.Major, packageSemVer.Minor, packageSemVer.Patch).ToString();
    }
    Information($"Project version: '{projectVersion}'.");
  });

Task("Clean")
  .IsDependentOn("Version")
  .Does(() => {
    var settings = new DockerComposeDownSettings {
      Rmi = "all"
    };
    DockerComposeDown(settings);
  });
