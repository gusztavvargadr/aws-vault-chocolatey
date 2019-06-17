FROM microsoft/aspnet@sha256:3c386c22b1a0aee1e0aa1ca11ad66b388d588861be0e3d5b506b74b0cdef5756

WORKDIR C:/work

ADD registry.client.ps1 .
RUN powershell -File ./registry.client.ps1

ADD registry.server.ps1 .
RUN  powershell -File ./registry.server.ps1
