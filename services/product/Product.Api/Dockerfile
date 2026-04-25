# =================== base
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 7002

# =================== build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# copy csproj (fore better cash)
COPY ["services/product/Product.Api/Product.Api.csproj", "services/product/Product.Api/"]

# restore
RUN dotnet restore "services/product/Product.Api/Product.Api.csproj"

# copy all
COPY . .

# go to progect folder
WORKDIR /src/services/product/Product.Api

# build
RUN dotnet build "Product.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# =================== publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "Product.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# =================== final
FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Product.Api.dll"]