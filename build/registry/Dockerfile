FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-ltsc2019

WORKDIR C:/work

ADD ./init.chocolatey-client.ps1 .
RUN powershell -File ./init.chocolatey-client.ps1

ADD ./init.chocolatey-server.ps1 .
RUN  powershell -File ./init.chocolatey-server.ps1
