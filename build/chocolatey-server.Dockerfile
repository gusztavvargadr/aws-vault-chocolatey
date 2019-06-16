FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-ltsc2019

WORKDIR /tmp

ADD chocolatey.ps1 .
RUN ./chocolatey.ps1

ADD chocolatey-server.ps1 .
RUN ./chocolatey-server.ps1
