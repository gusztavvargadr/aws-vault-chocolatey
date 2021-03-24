FROM mcr.microsoft.com/dotnet/sdk:3.1

WORKDIR C:/opt/docker/work/

RUN setx PATH "%PATH%;C:\Users\ContainerUser\.dotnet\tools"
RUN dotnet tool install --global gitversion.tool 

ENTRYPOINT [ "dotnet", "gitversion" ]
