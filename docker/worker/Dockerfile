# =========================
# Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CommercialNews.sln ./
COPY src ./src

RUN dotnet restore src/hosts/CommercialNews.Worker/CommercialNews.Worker.csproj

RUN dotnet publish src/hosts/CommercialNews.Worker/CommercialNews.Worker.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# =========================
# Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "CommercialNews.Worker.dll"]