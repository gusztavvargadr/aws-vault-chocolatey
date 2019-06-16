#load "core.cake"

Restored = () => {
  var upSettings = new DockerComposeUpSettings {
    DetachedMode = true
  };
  var service = "registry";
  DockerComposeUp(upSettings, service);
};

Task("Build")
  .IsDependentOn("Restore")
  .Does(() => {
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => {
  });

Task("Package")
  .IsDependentOn("Test")
  .Does(() => {
  });

Task("Publish")
  .IsDependentOn("Package")
  .Does(() => {
  });

RunTarget(target);
