# Multi-stage: a imagem final roda o app publicado, não o SDK (spine § Deployment).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Mundial.Dominio/ Mundial.Dominio/
COPY Mundial.Aplicacao/ Mundial.Aplicacao/
COPY Mundial.Infraestrutura/ Mundial.Infraestrutura/
COPY Mundial.Demo/ Mundial.Demo/
COPY Mundial.Api/ Mundial.Api/
COPY Mundial.Migrations/ Mundial.Migrations/
RUN dotnet publish Mundial.Api/Mundial.Api.csproj -c Release -o /app/api
RUN dotnet publish Mundial.Migrations/Mundial.Migrations.csproj -c Release -o /app/migracoes

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=build /app/api .
# service.version na telemetria. O deploy passa o commit (--build-arg VERSAO=$ALVO); é o que
# permite dizer em qual versão a latência mudou. Fica na imagem, não no .env: variável nova no
# .env da máquina não chega sozinha, e foi assim que a telemetria ficou desligada em produção.
ARG VERSAO=dev
ENV VERSAO=$VERSAO
ENV ASPNETCORE_HTTP_PORTS=5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "Mundial.Api.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migracoes
WORKDIR /app
COPY --from=build /app/migracoes .
ARG VERSAO=dev
ENV VERSAO=$VERSAO
ENTRYPOINT ["dotnet", "Mundial.Migrations.dll"]
