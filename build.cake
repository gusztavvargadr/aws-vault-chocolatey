#load "core.cake"

var sourceDirectory = Directory("./src");
var buildDirectory = Directory("./build");
var artifactsDirectory = Directory(Argument("artifacts-directory", "./artifacts"));

var appSourceRepository = "https://github.com/99designs/aws-vault";
Func<string> appDownloadUrl = () => $"{appSourceRepository}/releases/download/v{appVersion}/aws-vault-windows-386.exe";
var appDownloadFile = buildDirectory + File("tools/aws-vault.exe");

Task("Restore")
  .IsDependentOn("Version")
  .Does(() => {
    EnsureDirectoryExists(buildDirectory);

    CopyDirectory(sourceDirectory, buildDirectory);
    DownloadFile(appDownloadUrl(), appDownloadFile);
    foreach (var file in GetFiles(buildDirectory.Path + "/**/*template*")) {
      var template = FileReadText(file);
      var rendered = TransformText(template, "{", "}")
        .WithToken("version", packageVersion)
        .WithToken("licenseUrl", "https://github.com/99designs/aws-vault/blob/master/LICENSE")
        .WithToken("releaseNotes", $"https://github.com/99designs/aws-vault/releases/tag/v{appVersion}/")
        .WithToken("downloadUrl", appDownloadUrl())
        .WithToken("downloadHashMD5", CalculateFileHash(appDownloadFile, HashAlgorithm.MD5).ToHex().ToUpper())
        .WithToken("downloadHashSHA256", CalculateFileHash(appDownloadFile, HashAlgorithm.SHA256).ToHex().ToUpper())
        .WithToken("downloadHashSHA512", CalculateFileHash(appDownloadFile, HashAlgorithm.SHA512).ToHex().ToUpper())
        .ToString();
      FileWriteText(File(file.FullPath.Replace(".template.", ".")), rendered);

      DeleteFile(file);
    }

    EnsureDirectoryExists(artifactsDirectory);
  });

Task("Build")
  .IsDependentOn("Restore")
  .Does(() => {
    var settings = new ChocolateyPackSettings {
      WorkingDirectory = buildDirectory
    };

    ChocolateyPack(GetFiles(buildDirectory.Path + "/**/*.nuspec"), settings);
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => {
    // install and verify
  });

Task("Package")
  .IsDependentOn("Test")
  .Does(() => {
  });

Task("Publish")
  .IsDependentOn("Package")
  .Does(() => {
    CopyFiles(buildDirectory.Path + "/**/*.nupkg", artifactsDirectory);
  });

Task("Clean")
  .IsDependentOn("Version")
  .Does(() => {
    CleanDirectory(artifactsDirectory);

    CleanDirectory(buildDirectory);
  });

Task("Default")
  .IsDependentOn("Publish");

RunTarget(target);
