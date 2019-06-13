#addin "nuget:?package=Cake.SemVer&version=3.0.0"
#addin "nuget:?package=semver&version=2.0.4"
#addin "nuget:?package=Cake.FileHelpers&version=3.2.0"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var sourceVersion = Argument("source-version", string.Empty);
var buildVersion = Argument("build-version", string.Empty);
var appVersion = Argument("app-version", string.Empty);
var packageVersion = Argument("package-version", string.Empty);

Semver.SemVersion sourceSemVer;

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
          throw new Exception($"Error executing GitVersion '{process.GetExitCode()}'.");
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
    }
  });
