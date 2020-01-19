FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-ltsc2019

WORKDIR C:/opt/chocolatey/

ADD ./chocolatey-server.init.ps1 ./
RUN powershell -File ./chocolatey-server.init.ps1

ADD ./chocolatey-server.install.ps1 ./
RUN powershell -File ./chocolatey-server.install.ps1
