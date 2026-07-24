# syntax=docker/dockerfile:1

# ----- Build -----
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY src/Chamados.Api/Chamados.Api.csproj src/Chamados.Api/
RUN dotnet restore src/Chamados.Api/Chamados.Api.csproj

COPY src/Chamados.Api/ src/Chamados.Api/
RUN dotnet publish src/Chamados.Api/Chamados.Api.csproj -c Release -o /app/publish --no-restore

# ----- Runtime -----
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

RUN addgroup -S chamados && adduser -S chamados -G chamados

# Criado com dono correto antes do USER para que o volume nomeado montado em
# /app/uploads (docker-compose) herde essa permissão de escrita ao inicializar.
RUN mkdir -p /app/uploads && chown -R chamados:chamados /app/uploads

USER chamados

COPY --from=build --chown=chamados:chamados /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Chamados.Api.dll"]
