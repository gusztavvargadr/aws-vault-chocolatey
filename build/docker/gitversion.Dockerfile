FROM mcr.microsoft.com/dotnet/sdk:6.0

WORKDIR C:/opt/docker/work/

RUN dotnet tool install --global --version 5.12.0 gitversion.tool

ENTRYPOINT %USERPROFILE%\.dotnet\tools\dotnet-gitversion.exe
