# Build context: raiz do repositório (ver docker-compose.yml).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore em camada separada para aproveitar o cache do Docker.
COPY Directory.Build.props global.json ./
COPY src/Transfers.Domain/Transfers.Domain.csproj src/Transfers.Domain/
COPY src/Transfers.Application/Transfers.Application.csproj src/Transfers.Application/
COPY src/Transfers.Infrastructure/Transfers.Infrastructure.csproj src/Transfers.Infrastructure/
COPY src/Transfers.Api/Transfers.Api.csproj src/Transfers.Api/
RUN dotnet restore src/Transfers.Api/Transfers.Api.csproj

COPY src/ src/
RUN dotnet publish src/Transfers.Api/Transfers.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# tzdata: resolve America/Sao_Paulo (período dia/noite). curl: healthcheck do docker-compose.
RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER app

ENTRYPOINT ["dotnet", "Transfers.Api.dll"]
