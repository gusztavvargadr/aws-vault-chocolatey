#addin "nuget:?package=Cake.Docker&version=0.10.0"
#addin "nuget:?package=Cake.SemVer&version=3.0.0"
#addin "nuget:?package=semver&version=2.0.4"
#addin "nuget:?package=Cake.FileHelpers&version=3.2.0"

var target = Argument("target", "Publish");
var configuration = Argument("configuration", "Release");

var sourceVersion = Argument("source-version", string.Empty);
Semver.SemVersion sourceSemVer;
var buildVersion = Argument("build-version", string.Empty);
var appVersion = Argument("app-version", string.Empty);
var packageVersion = Argument("package-version", string.Empty);

var sourceDirectory = Directory(Argument("source-directory", "./src"));
var workDirectory = Directory(Argument("work-directory", "./work"));
var artifactsDirectory = Directory(Argument("artifacts-directory", "./artifacts"));

Action Versioned = () => {
};

Task("Version")
  .Does(context => {
    try {
      if (!string.IsNullOrEmpty(sourceVersion)) {
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
          throw new Exception($"Error executing 'GitVersion': '{process.GetExitCode()}'.");
        }

        sourceVersion = string.Join(Environment.NewLine, process.GetStandardOutput());
      }
    } finally {
      sourceSemVer = ParseSemVer(sourceVersion);

      if (string.IsNullOrEmpty(buildVersion)) {
        buildVersion = $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
      }
      if (string.IsNullOrEmpty(appVersion)) {
        appVersion = new Semver.SemVersion(sourceSemVer.Major, sourceSemVer.Minor, sourceSemVer.Patch).ToString();
      }
      if (string.IsNullOrEmpty(packageVersion)) {
        packageVersion = appVersion;
      }

      Information($"Source: '{sourceVersion}'.");
      Information($"Build: '{buildVersion}'.");
      Information($"App: '{appVersion}'.");
      Information($"Package: '{packageVersion}'.");

      Versioned();
    }
  });

Action Restored = () => {};

Task("Restore")
  .IsDependentOn("Version")
  .Does(() => {
    EnsureDirectoryExists(workDirectory);
    EnsureDirectoryExists(artifactsDirectory);

    Restored();
  });

Action Cleaned = () => {};

Task("Clean")
  .IsDependentOn("Version")
  .Does(() => {
    var downSettings = new DockerComposeDownSettings {
      Rmi = "all"
    };
    DockerComposeDown(downSettings);

    CleanDirectory(artifactsDirectory);
    CleanDirectory(workDirectory);

    Cleaned();
  });
