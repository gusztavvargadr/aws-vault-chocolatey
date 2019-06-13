#addin "nuget:?package=Cake.Docker&version=0.10.0"
#addin "nuget:?package=Cake.SemVer&version=3.0.0"
#addin "nuget:?package=semver&version=2.0.4"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var version = Argument("build-version", string.Empty);
Semver.SemVersion semanticVersion;

Task("Version")
  .Does(context => {
    try {
      if (!string.IsNullOrEmpty(version)) {
        return;
      }

      var settings = new DockerComposeUpSettings {
      };
      var service = "gitversion";

      DockerComposeUp(settings, service);

      var output = DockerComposeLogs(context, new DockerComposeLogsSettings { NoColor = true }, service);
      version = output.Split(Environment.NewLine).Last().Split('|').Last().Trim().Replace("-rc-origin-", "-rc-");
    } finally {
      Information(version);

      semanticVersion = ParseSemVer(version);
    }
  });

Task("Restore")
  .IsDependentOn("Version")
  .Does(() => {
    var settings = new DockerComposePullSettings {
      IgnorePullFailures = true
    };

    DockerComposePull(settings);
  });

Task("Clean")
  .IsDependentOn("Version")
  .Does(() => {
    var settings = new DockerComposeDownSettings {
      Rmi = "all"
    };

    DockerComposeDown(settings);
  });

private string DockerComposeLogs(ICakeContext context, DockerComposeLogsSettings settings, string service) {
  var runner = new GenericDockerComposeRunner<DockerComposeLogsSettings>(
    context.FileSystem,
    context.Environment,
    context.ProcessRunner,
    context.Tools
  );

  var output = runner.RunWithResult<string>("logs", settings, (processOutput) => processOutput.ToArray(), service);

  return string.Join(Environment.NewLine, output);
}
