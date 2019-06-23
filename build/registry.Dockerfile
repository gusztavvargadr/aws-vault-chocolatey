FROM microsoft/aspnet:latest

WORKDIR C:/work

ADD registry.init-chocolatey-client.ps1 .
RUN powershell -File ./registry.init-chocolatey-client.ps1

ADD registry.init-chocolatey-server.ps1 .
RUN  powershell -File ./registry.init-chocolatey-server.ps1
