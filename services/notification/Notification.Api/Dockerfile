# base
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
ARG APP_UID=1000
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
RUN id appuser 2>/dev/null || (useradd -m -u ${APP_UID} appuser 2>/dev/null || useradd -m appuser)
USER appuser
WORKDIR /app
EXPOSE 7004

# =================== build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# for better cash
COPY ["services/notification/Notification.Api/Notification.Api.csproj", "services/notification/Notification.Api/"]

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