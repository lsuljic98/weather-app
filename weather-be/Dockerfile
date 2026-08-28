# syntax=docker/dockerfile:1

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# csproj + sln first so `restore` is cached until dependencies actually change
COPY WeatherApp.sln .
COPY WeatherApp.API/WeatherApp.API.csproj                   WeatherApp.API/
COPY WeatherApp.Application/WeatherApp.Application.csproj   WeatherApp.Application/
COPY WeatherApp.Domain/WeatherApp.Domain.csproj             WeatherApp.Domain/
COPY WeatherApp.Infrastructure/WeatherApp.Infrastructure.csproj WeatherApp.Infrastructure/
RUN dotnet restore

COPY . .
RUN dotnet publish WeatherApp.API -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- dev (hot reload, used by docker-compose.override.yml) ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dev
WORKDIR /src
ENV DOTNET_USE_POLLING_FILE_WATCHER=1 \
    ASPNETCORE_ENVIRONMENT=Development \
    ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
CMD ["dotnet", "watch", "run", "--project", "WeatherApp.API", \
     "--no-launch-profile", "--urls", "http://0.0.0.0:8080"]

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_gcServer=1
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "WeatherApp.API.dll"]
