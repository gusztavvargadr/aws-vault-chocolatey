FROM mcr.microsoft.com/dotnet/sdk:3.1

WORKDIR C:/opt/docker/work/

RUN set
RUN setx PATH "%PATH%;%HOME%\.dotnet\tools"
RUN dotnet tool install --version 5.6.6 --global gitversion.tool 

ENTRYPOINT [ "dotnet", "gitversion" ]
