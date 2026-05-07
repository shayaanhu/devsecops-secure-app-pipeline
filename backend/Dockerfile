# ── Stage 1: Build ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY UniRide.Api.csproj ./
RUN dotnet restore UniRide.Api.csproj

COPY . .
RUN dotnet publish UniRide.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ───────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

# SQLite database lives on the mounted volume at /data/
VOLUME ["/data"]

ENV ASPNETCORE_URLS=http://0.0.0.0:5007
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5007

ENTRYPOINT ["dotnet", "UniRide.Api.dll"]
