FROM mcr.microsoft.com/windows/servercore:ltsc2022

WORKDIR C:/opt/docker/work/

RUN powershell -Command "$env:chocolateyVersion = \"2.5.0\"; Set-ExecutionPolicy Bypass -Scope Process -Force; Invoke-WebRequest https://chocolatey.org/install.ps1 -UseBasicParsing | Invoke-Expression"
