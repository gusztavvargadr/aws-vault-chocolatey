#load "./build/core.cake"

Restored = () => {
  CopyDirectory(sourceDirectory, workDirectory);
  DownloadFile(appDownloadUrl(), packageFile);

  foreach (var file in GetFiles(workDirectory.Path + "/**/*template*")) {
    var template = FileReadText(file);

    var rendered = TransformText(template, "{", "}")
      .WithToken("id", packageName)
      .WithToken("version", packageVersion)
      .WithToken("projectSourceUrl", $"{appSourceRepository}")
      .WithToken("licenseUrl", $"{appSourceRepository}/blob/master/LICENSE")
      .WithToken("releaseNotes", $"{appSourceRepository}/releases/tag/v{appVersion}/")
      .WithToken("downloadUrl", appDownloadUrl())
      .WithToken("downloadHashMD5", CalculateFileHash(packageFile, HashAlgorithm.MD5).ToHex().ToUpper())
      .WithToken("downloadHashSHA256", CalculateFileHash(packageFile, HashAlgorithm.SHA256).ToHex().ToUpper())
      .WithToken("downloadHashSHA512", CalculateFileHash(packageFile, HashAlgorithm.SHA512).ToHex().ToUpper())
      .ToString();
    FileWriteText(File(file.FullPath.Replace(".template.", ".")), rendered);

    DeleteFile(file);
  }
};

Task("Build")
  .IsDependentOn("Restore")
  .Does(() => {
    var packSettings = new ChocolateyPackSettings {
      RequireLicenseAcceptance = true,
      WorkingDirectory = workDirectory
    };
    ChocolateyPack(GetFiles(workDirectory.Path + "/**/*.nuspec"), packSettings);
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => {
    var installSettings = new ChocolateyInstallSettings {
      Debug = true,
      Verbose = true,
      Source = ".",
      WorkingDirectory = workDirectory,
      Prerelease = !string.IsNullOrEmpty(sourceSemVer.Prerelease)
    };
    ChocolateyInstall(packageName, installSettings);

    using(var process = StartAndReturnProcess(
      packageFilename,
      new ProcessSettings {
        Arguments = "--version",
        RedirectStandardOutput = true
      }
    )) {
      process.WaitForExit();
      if (process.GetExitCode() != 0) {
        throw new Exception($"Error executing '{packageFilename}': '{process.GetExitCode()}'.");
      }

      var actualVersion = string.Join(Environment.NewLine, process.GetStandardOutput());
      Information($"Actual version: '{actualVersion}'.");
      var expectedVersion = $"v{appVersion}";
      if (actualVersion != expectedVersion) {
        throw new Exception($"Actual version '{actualVersion}' does not match expected version '{expectedVersion}'.");
      }
    }

    var uninstallSettings = new ChocolateyUninstallSettings {
    };
    ChocolateyUninstall(packageName, uninstallSettings);
  });

Task("Package")
  .IsDependentOn("Test")
  .Does(() => {
  });

Task("Publish")
  .IsDependentOn("Package")
  .Does(() => {
    CopyFiles(workDirectory.Path + $"/**/{packageName}.{packageVersion}.nupkg", artifactsDirectory);

    Information($"Copied artifacts to '{artifactsDirectory}'.");
  });

RunTarget(target);
