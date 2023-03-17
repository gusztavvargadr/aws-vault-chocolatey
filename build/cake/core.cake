#addin nuget:?package=Cake.Docker&version=1.1.2
#addin nuget:?package=Cake.SemVer&version=4.0.0
#addin nuget:?package=semver&version=2.0.4

var target = Argument("target", "Publish");
var packageName = "aws-vault";

var sourceVersion = Argument("source-version", "6.6.2");
Semver.SemVersion sourceSemVer;
var buildVersion = Argument("build-version", string.Empty);
var projectVersion = Argument("project-version", string.Empty);
var packageVersion = Argument("package-version", string.Empty);

var chocolateyServer = EnvironmentVariable("CHOCOLATEY_SERVER", string.Empty);

Task("Init")
  .Does(() => {
    StartProcess("docker", "--version");
    StartProcess("docker-compose", "--version");

    StartProcess("docker", "system df");
    StartProcess("docker", "container ls -a");
    StartProcess("docker", "image ls -a");
  });

Task("Restore")
  .IsDependentOn("Init")
  .Does(() => {
    var settings = new DockerComposeBuildSettings {
    };
    var services = new [] { "chef-client", "chocolatey" };
    if (string.IsNullOrEmpty(sourceVersion)) {
      services = (new [] { "gitversion" }).Concat(services).ToArray();
    }
    DockerComposeBuild(settings, services);
  });

Task("Version")
  .IsDependentOn("Restore")
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

Task("Clean")
  .IsDependentOn("Init")
  .Does(() => {
    StartProcess("docker", "container prune -f");

    var settings = new DockerComposeDownSettings {
      Rmi = "local",
      Volumes = true,
    };
    DockerComposeDown(settings);

    StartProcess("docker", "image prune -f");
    StartProcess("docker", "builder prune -af");

    CleanDirectory("./artifacts/");
  });
