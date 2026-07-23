# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /src

COPY src/AgentGovernanceDemo/AgentGovernanceDemo.csproj src/AgentGovernanceDemo/
RUN dotnet restore src/AgentGovernanceDemo/AgentGovernanceDemo.csproj

COPY src/AgentGovernanceDemo/ src/AgentGovernanceDemo/
RUN dotnet publish src/AgentGovernanceDemo/AgentGovernanceDemo.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080

COPY --from=build --chown=app:app /app/publish .
USER app
ENTRYPOINT ["dotnet", "AgentGovernanceDemo.dll"]
