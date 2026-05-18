FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY archlens-contracts/Directory.Build.props ./archlens-contracts/
COPY archlens-contracts/src/ArchLens.SharedKernel/*.csproj ./archlens-contracts/src/ArchLens.SharedKernel/
COPY archlens-contracts/src/ArchLens.Contracts/*.csproj ./archlens-contracts/src/ArchLens.Contracts/

COPY archlens-orchestrator-service/*.sln ./archlens-orchestrator-service/
COPY archlens-orchestrator-service/Directory.Build.props ./archlens-orchestrator-service/
COPY archlens-orchestrator-service/src/ArchLens.Orchestrator.Api/*.csproj ./archlens-orchestrator-service/src/ArchLens.Orchestrator.Api/
COPY archlens-orchestrator-service/src/ArchLens.Orchestrator.Application/*.csproj ./archlens-orchestrator-service/src/ArchLens.Orchestrator.Application/
COPY archlens-orchestrator-service/src/ArchLens.Orchestrator.Application.Contracts/*.csproj ./archlens-orchestrator-service/src/ArchLens.Orchestrator.Application.Contracts/
COPY archlens-orchestrator-service/src/ArchLens.Orchestrator.Domain/*.csproj ./archlens-orchestrator-service/src/ArchLens.Orchestrator.Domain/
COPY archlens-orchestrator-service/src/ArchLens.Orchestrator.Infrastructure/*.csproj ./archlens-orchestrator-service/src/ArchLens.Orchestrator.Infrastructure/

WORKDIR /src/archlens-orchestrator-service
RUN dotnet restore src/ArchLens.Orchestrator.Api/ArchLens.Orchestrator.Api.csproj

WORKDIR /src
COPY archlens-contracts/ ./archlens-contracts/
COPY archlens-orchestrator-service/ ./archlens-orchestrator-service/

WORKDIR /src/archlens-orchestrator-service
RUN dotnet publish src/ArchLens.Orchestrator.Api -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
LABEL org.opencontainers.image.source="https://github.com/archlens-platform/archlens-orchestrator-service"
LABEL org.opencontainers.image.title="ArchLens Orchestrator Service"
LABEL org.opencontainers.image.version="1.0.0"
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

USER $APP_UID
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=5s --start-period=30s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ArchLens.Orchestrator.Api.dll"]
