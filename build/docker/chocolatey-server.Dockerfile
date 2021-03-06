FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-ltsc2019

WORKDIR C:/opt/chocolatey/

ENV chocolateyVersion 0.10.15
RUN iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
RUN choco config set cacheLocation C:\tmp\choco

ADD ./chocolatey-server.install.ps1 ./
RUN ./chocolatey-server.install.ps1
