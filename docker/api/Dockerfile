# =========================
# Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CommercialNews.sln ./
COPY src ./src

RUN dotnet restore src/hosts/CommercialNews.Api/CommercialNews.Api.csproj

RUN dotnet publish src/hosts/CommercialNews.Api/CommercialNews.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# =========================
# Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "CommercialNews.Api.dll"]