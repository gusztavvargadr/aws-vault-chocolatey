FROM mcr.microsoft.com/dotnet/sdk:3.1

WORKDIR C:/opt/docker/work/

USER ContainerAdministrator
RUN setx /M PATH "%PATH%;C:\Program Files\dotnet\tools"
USER ContainerUser

RUN dotnet tool install --version 5.6.6 --tool-path "C:\Program Files\dotnet\tools" gitversion.tool 

ENTRYPOINT [ "dotnet", "gitversion" ]
