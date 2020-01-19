FROM mcr.microsoft.com/windows/servercore:ltsc2019

WORKDIR C:/opt/chocolatey/

ENV chocolateyVersion 0.10.15
RUN powershell -Command iex ((new-object net.webclient).DownloadString('https://chocolatey.org/install.ps1'));
RUN choco config set cacheLocation C:\tmp\choco

ENTRYPOINT [ "choco" ]
CMD [ "--help" ]
