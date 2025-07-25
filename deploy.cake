#load ./build/cake/core.cake

Task("Build")
  .IsDependentOn("Version")
  .Does(() => {
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => {
    StartProcess("docker", $"compose run --entrypoint \"powershell -File ./build/docker/chocolatey.package.install.ps1\" chocolatey {packageVersion}");
  });

Task("Package")
  .IsDependentOn("Test")
  .Does(() => {
  });

Task("Publish")
  .IsDependentOn("Package")
  .Does(() => {
    if (string.IsNullOrEmpty(chocolateyServer)) {
      return;
    }

    StartProcess("docker", $"compose run --entrypoint \"powershell -File ./build/docker/chocolatey.package.push.ps1\" chocolatey {packageVersion}");
  });

RunTarget(target);
