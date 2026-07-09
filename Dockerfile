# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props global.json ./
COPY src/PulseRisk.Domain/PulseRisk.Domain.csproj src/PulseRisk.Domain/packages.lock.json src/PulseRisk.Domain/
COPY src/PulseRisk.Application/PulseRisk.Application.csproj src/PulseRisk.Application/packages.lock.json src/PulseRisk.Application/
COPY src/PulseRisk.Infrastructure/PulseRisk.Infrastructure.csproj src/PulseRisk.Infrastructure/packages.lock.json src/PulseRisk.Infrastructure/
COPY src/PulseRisk.BackgroundWorkers/PulseRisk.BackgroundWorkers.csproj src/PulseRisk.BackgroundWorkers/packages.lock.json src/PulseRisk.BackgroundWorkers/
COPY src/PulseRisk.Api/PulseRisk.Api.csproj src/PulseRisk.Api/packages.lock.json src/PulseRisk.Api/

RUN dotnet restore src/PulseRisk.Api/PulseRisk.Api.csproj --locked-mode

COPY src/ src/
RUN dotnet publish src/PulseRisk.Api/PulseRisk.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "PulseRisk.Api.dll"]
