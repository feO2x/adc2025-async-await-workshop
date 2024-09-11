FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine3.19 AS prepare
WORKDIR /app
RUN adduser -u 1000 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine3.19 AS build
WORKDIR /src
COPY ["./AsyncVsSync.Backend.csproj", "./AsyncVsSync.Backend.csproj"]
RUN dotnet restore "./AsyncVsSync.Backend.csproj"
COPY . .
RUN dotnet publish "./AsyncVsSync.Backend.csproj" \
    -c Release \
    --no-restore \
    -o "../app"

FROM prepare AS final
COPY --chown=appuser --from=build /app .
ENTRYPOINT ["dotnet", "AsyncVsSync.Backend.dll"]
