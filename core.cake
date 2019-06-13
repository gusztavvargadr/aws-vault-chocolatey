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

      using(var process = StartAndReturnProcess(
        "dotnet",
        new ProcessSettings {
          Arguments = $"gitversion {context.Environment.WorkingDirectory} /showvariable SemVer",
          RedirectStandardOutput = true
        }
      )) {
        process.WaitForExit();
        if (process.GetExitCode() != 0) {
          throw new Exception($"Error executing GitVersion '{process.GetExitCode()}'.");
        }

        version = string.Join(Environment.NewLine, process.GetStandardOutput());
      }
    } finally {
      Information($"Version: '{version}'.");

      semanticVersion = ParseSemVer(version);
    }
  });

Task("Restore")
  .IsDependentOn("Version")
  .Does(() => {
  });

Task("Clean")
  .IsDependentOn("Version")
  .Does(() => {
    var settings = new DockerComposeDownSettings {
      Rmi = "all"
    };

    DockerComposeDown(settings);
  });
