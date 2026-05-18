FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Build.props .

COPY src/SharedKernel/ArchLens.SharedKernel/ArchLens.SharedKernel.csproj src/SharedKernel/ArchLens.SharedKernel/
COPY src/Contracts/ArchLens.Contracts/ArchLens.Contracts.csproj src/Contracts/ArchLens.Contracts/
COPY src/Services/Orchestrator/ArchLens.Orchestrator.Domain/ArchLens.Orchestrator.Domain.csproj src/Services/Orchestrator/ArchLens.Orchestrator.Domain/
COPY src/Services/Orchestrator/ArchLens.Orchestrator.Application.Contracts/ArchLens.Orchestrator.Application.Contracts.csproj src/Services/Orchestrator/ArchLens.Orchestrator.Application.Contracts/
COPY src/Services/Orchestrator/ArchLens.Orchestrator.Application/ArchLens.Orchestrator.Application.csproj src/Services/Orchestrator/ArchLens.Orchestrator.Application/
COPY src/Services/Orchestrator/ArchLens.Orchestrator.Infrastructure/ArchLens.Orchestrator.Infrastructure.csproj src/Services/Orchestrator/ArchLens.Orchestrator.Infrastructure/
COPY src/Services/Orchestrator/ArchLens.Orchestrator.Api/ArchLens.Orchestrator.Api.csproj src/Services/Orchestrator/ArchLens.Orchestrator.Api/

RUN dotnet restore src/Services/Orchestrator/ArchLens.Orchestrator.Api/ArchLens.Orchestrator.Api.csproj

COPY src/SharedKernel/ src/SharedKernel/
COPY src/Contracts/ src/Contracts/
COPY src/Services/Orchestrator/ src/Services/Orchestrator/

RUN dotnet publish src/Services/Orchestrator/ArchLens.Orchestrator.Api/ArchLens.Orchestrator.Api.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
USER $APP_UID
ENTRYPOINT ["dotnet", "ArchLens.Orchestrator.Api.dll"]
