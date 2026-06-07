
# base
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
ARG APP_UID=1000
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
RUN id appuser 2>/dev/null || (useradd -m -u ${APP_UID} appuser 2>/dev/null || useradd -m appuser)
USER appuser
WORKDIR /app
EXPOSE 7000
#EXPOSE 7001

# =================== build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
#FROM mcr-mirror.liara.ir/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# for better cash
COPY ["gateway/Gateway.Api/Gateway.Api.csproj", "gateway/Gateway.Api/"]
#COPY ["building-blocks/BuildingBlocks/BuildingBlocks.Api.csproj", "BuildingBlocks/BuildingBlocks.Api/"]

RUN dotnet restore "gateway/Gateway.Api/Gateway.Api.csproj"
COPY . .

WORKDIR /src/gateway/Gateway.Api
RUN dotnet build "Gateway.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Gateway.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gateway.Api.dll"]