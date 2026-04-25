
# base
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
#FROM mcr-mirror.liara.ir/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 7003
#EXPOSE 7001

# =================== build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
#FROM mcr-mirror.liara.ir/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# for better cash
COPY ["services/order/Order.Api/Order.Api.csproj", "services/order/Order.Api/"]

RUN dotnet restore "services/order/Order.Api/Order.Api.csproj"
COPY . .

WORKDIR /src/services/order/Order.Api
RUN dotnet build "Order.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Order.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Order.Api.dll"]