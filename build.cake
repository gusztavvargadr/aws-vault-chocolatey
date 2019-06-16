#load "core.cake"

var appSourceRepository = "https://github.com/99designs/aws-vault";
Func<string> appDownloadUrl = () => $"{appSourceRepository}/releases/download/v{appVersion}/aws-vault-windows-386.exe";
var appDownloadFile = workDirectory + File("tools/aws-vault.exe");

var packageId = "aws-vault";

Restored = () => {
  CopyDirectory(sourceDirectory, workDirectory);
  DownloadFile(appDownloadUrl(), appDownloadFile);
};

Task("Build")
  .IsDependentOn("Restore")
  .Does(() => {
    foreach (var file in GetFiles(workDirectory.Path + "/**/*template*")) {
      var template = FileReadText(file);
      var rendered = TransformText(template, "{", "}")
        .WithToken("id", packageId)
        .WithToken("version", packageVersion)
        .WithToken("projectSourceUrl", $"{appSourceRepository}")
        .WithToken("licenseUrl", $"{appSourceRepository}/blob/master/LICENSE")
        .WithToken("releaseNotes", $"{appSourceRepository}/releases/tag/v{appVersion}/")
        .WithToken("downloadUrl", appDownloadUrl())
        .WithToken("downloadHashMD5", CalculateFileHash(appDownloadFile, HashAlgorithm.MD5).ToHex().ToUpper())
        .WithToken("downloadHashSHA256", CalculateFileHash(appDownloadFile, HashAlgorithm.SHA256).ToHex().ToUpper())
        .WithToken("downloadHashSHA512", CalculateFileHash(appDownloadFile, HashAlgorithm.SHA512).ToHex().ToUpper())
        .ToString();
      FileWriteText(File(file.FullPath.Replace(".template.", ".")), rendered);

      DeleteFile(file);
    }

    var settings = new ChocolateyPackSettings {
      RequireLicenseAcceptance = true,
      WorkingDirectory = workDirectory
    };

    ChocolateyPack(GetFiles(workDirectory.Path + "/**/*.nuspec"), settings);
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => {
    var settings = new ChocolateyInstallSettings {
      Debug = true,
      Verbose = true,
      Source = ".",
      WorkingDirectory = workDirectory
    };

    ChocolateyInstall(packageId, settings);

    using(var process = StartAndReturnProcess(
      packageId,
      new ProcessSettings {
        Arguments = "--version",
        RedirectStandardOutput = true
      }
    )) {
      process.WaitForExit();
      if (process.GetExitCode() != 0) {
        throw new Exception($"Error executing '{packageId}': '{process.GetExitCode()}'.");
      }

      var actualVersion = string.Join(Environment.NewLine, process.GetStandardOutput());
      var expectedVersion = $"v{appVersion}";
      if (actualVersion != expectedVersion) {
        throw new Exception($"Actual version '{actualVersion}' does not match expected version '{expectedVersion}'.");
      }
    }
  });

Task("Package")
  .IsDependentOn("Test")
  .Does(() => {
  });

Task("Publish")
  .IsDependentOn("Package")
  .Does(() => {
    CopyFiles(workDirectory.Path + "/**/*.nupkg", artifactsDirectory);
  });

Cleaned = () => {
  ChocolateyUninstall(packageId);
};

RunTarget(target);
