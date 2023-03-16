FROM mcr.microsoft.com/dotnet/sdk:6.0

WORKDIR C:/opt/docker/work/

USER ContainerAdministrator
RUN setx /M PATH "%PATH%;C:\Program Files\dotnet\tools"
USER ContainerUser

RUN dotnet tool install --version 5.12.0 --tool-path "C:\Program Files\dotnet\tools" gitversion.tool 

ENTRYPOINT [ "dotnet", "gitversion" ]
