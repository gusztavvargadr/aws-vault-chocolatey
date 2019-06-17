FROM microsoft/aspnet@sha256:3c386c22b1a0aee1e0aa1ca11ad66b388d588861be0e3d5b506b74b0cdef5756

WORKDIR /tmp

ADD chocolatey.ps1 .
RUN ./chocolatey.ps1

ADD chocolatey-server.ps1 .
RUN ./chocolatey-server.ps1
