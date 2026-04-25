
# base
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 7004
#EXPOSE 7001

# =================== build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
#FROM mcr-mirror.liara.ir/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# for better cash
COPY ["services/notification/Notification.Api/Notification.Api.csproj", "services/notification/Notification.Api/"]
#COPY ["building-blocks/BuildingBlocks/BuildingBlocks.Api.csproj", "BuildingBlocks/BuildingBlocks.Api/"]

RUN dotnet restore "services/notification/Notification.Api/Notification.Api.csproj"
COPY . .

WORKDIR /src/services/notification/Notification.Api
RUN dotnet build "Notification.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Notification.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Notification.Api.dll"]