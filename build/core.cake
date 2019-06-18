#addin "nuget:?package=Cake.Docker&version=0.10.0"
#addin "nuget:?package=Cake.SemVer&version=3.0.0"
#addin "nuget:?package=semver&version=2.0.4"
#addin "nuget:?package=Cake.FileHelpers&version=3.2.0"

var target = Argument("target", "Publish");
var configuration = Argument("configuration", "Release");

var sourceVersion = Argument("source-version", string.Empty);
var buildVersion = Argument("build-version", string.Empty);
var appVersion = Argument("app-version", "4.6.0");
var packageVersion = Argument("package-version", string.Empty);
Semver.SemVersion sourceSemVer;

var sourceDirectory = Directory(Argument("source-directory", "./src"));
var workDirectory = Directory(Argument("work-directory", "./work"));
var artifactsDirectory = Directory(Argument("artifacts-directory", "./artifacts"));

var appSourceRepository = "https://github.com/99designs/aws-vault";
Func<string> appDownloadUrl = () => $"{appSourceRepository}/releases/download/v{appVersion}/aws-vault-windows-386.exe";

var packageName = Argument("package-name", "aws-vault");
var packageRegistryPush = Argument("package-registry-push", "http://localhost:5000/chocolatey");
var packageRegistryPull = Argument("package-registry-pull", "http://localhost:5000/chocolatey");
var packageFilename = "aws-vault.exe";
var packageFile  = workDirectory + File($"tools/{packageFilename}");

Task("Version")
  .Does(() => {
    sourceVersion = !string.IsNullOrEmpty(sourceVersion) ? sourceVersion : GetSourceVersion();
    buildVersion = !string.IsNullOrEmpty(buildVersion) ? buildVersion : GetBuildVersion();
    appVersion = !string.IsNullOrEmpty(appVersion) ? appVersion : GetAppVersion();
    packageVersion = !string.IsNullOrEmpty(packageVersion) ? packageVersion : GetPackageVersion();
    sourceSemVer = ParseSemVer(sourceVersion);

    Information($"Source: '{sourceVersion}'.");
    Information($"Build: '{buildVersion}'.");
    Information($"App: '{appVersion}'.");
    Information($"Package: '{packageVersion}'.");

    Versioned();
  });

private string GetSourceVersion() {
  using(var process = StartAndReturnProcess(
    "dotnet",
    new ProcessSettings {
      Arguments = $"gitversion /showvariable NuGetVersionV2",
      RedirectStandardOutput = true
    }
  )) {
    process.WaitForExit();
    if (process.GetExitCode() != 0) {
      throw new Exception($"Error executing 'GitVersion': '{process.GetExitCode()}'.");
    }

    return string.Join(Environment.NewLine, process.GetStandardOutput());
  }
}

Func<string> GetBuildVersion = () => {
  return $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
};

Func<string> GetAppVersion = () => {
  return sourceVersion;
};

Func<string> GetPackageVersion = () => {
  return sourceVersion;
};

Action Versioned = () => {};

Task("Restore")
  .IsDependentOn("Version")
  .Does(() => {
    EnsureDirectoryExists(workDirectory);
    EnsureDirectoryExists(artifactsDirectory);

    Restored();
  });

Action Restored = () => {};

Task("Clean")
  .IsDependentOn("Version")
  .Does(() => {
    var downSettings = new DockerComposeDownSettings {
      Rmi = "all"
    };
    DockerComposeDown(downSettings);

    CleanDirectory(workDirectory);

    Cleaned();
  });

Action Cleaned = () => {};
