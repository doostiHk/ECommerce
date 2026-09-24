# Multi-stage build including BuildingBlocks project references.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
ARG APP_UID=1000
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
RUN id appuser 2>/dev/null || (useradd -m -u ${APP_UID} appuser 2>/dev/null || useradd -m appuser)
USER appuser
WORKDIR /app
EXPOSE 7002

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["nuget.config", "nuget.config"]
COPY ["building-blocks/BuildingBlocks.Common/BuildingBlocks.Common.csproj", "building-blocks/BuildingBlocks.Common/"]
COPY ["building-blocks/BuildingBlocks.Contracts/BuildingBlocks.Contracts.csproj", "building-blocks/BuildingBlocks.Contracts/"]
COPY ["building-blocks/BuildingBlocks.Persistence/BuildingBlocks.Persistence.csproj", "building-blocks/BuildingBlocks.Persistence/"]
COPY ["building-blocks/BuildingBlocks.Authentication/BuildingBlocks.Authentication.csproj", "building-blocks/BuildingBlocks.Authentication/"]
COPY ["services/product/Product.Api/Product.Api.csproj", "services/product/Product.Api/"]

RUN --mount=type=cache,target=/root/.nuget/packages \
    for i in 1 2 3 4 5 6; do \
      dotnet restore "services/product/Product.Api/Product.Api.csproj" && break || sleep 15; \
    done

COPY ["building-blocks/", "building-blocks/"]
COPY ["services/product/Product.Api/", "services/product/Product.Api/"]

WORKDIR /src/services/product/Product.Api
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "Product.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Product.Api.dll"]
