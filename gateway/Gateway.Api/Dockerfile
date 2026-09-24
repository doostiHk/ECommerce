# Multi-stage build including BuildingBlocks project references.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
ARG APP_UID=1000
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
RUN id appuser 2>/dev/null || (useradd -m -u ${APP_UID} appuser 2>/dev/null || useradd -m appuser)
USER appuser
WORKDIR /app
EXPOSE 7000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["nuget.config", "nuget.config"]
COPY ["building-blocks/BuildingBlocks.Common/BuildingBlocks.Common.csproj", "building-blocks/BuildingBlocks.Common/"]
COPY ["building-blocks/BuildingBlocks.Authentication/BuildingBlocks.Authentication.csproj", "building-blocks/BuildingBlocks.Authentication/"]
COPY ["gateway/Gateway.Api/Gateway.Api.csproj", "gateway/Gateway.Api/"]

RUN --mount=type=cache,target=/root/.nuget/packages \
    for i in 1 2 3 4 5 6; do \
      dotnet restore "gateway/Gateway.Api/Gateway.Api.csproj" && break || sleep 15; \
    done

COPY ["building-blocks/BuildingBlocks.Common/", "building-blocks/BuildingBlocks.Common/"]
COPY ["building-blocks/BuildingBlocks.Authentication/", "building-blocks/BuildingBlocks.Authentication/"]
COPY ["gateway/Gateway.Api/", "gateway/Gateway.Api/"]

WORKDIR /src/gateway/Gateway.Api
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "Gateway.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Gateway.Api.dll"]
