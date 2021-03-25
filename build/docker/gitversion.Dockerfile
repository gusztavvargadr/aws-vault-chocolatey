FROM mcr.microsoft.com/dotnet/sdk:3.1

WORKDIR C:/opt/docker/work/

RUN md %USERPROFILE%\.dotnet\tools
RUN setx PATH "%PATH%;%USERPROFILE%\.dotnet\tools"
RUN dotnet tool install --version 5.6.6 --global gitversion.tool 

ENTRYPOINT [ "dotnet", "gitversion" ]
