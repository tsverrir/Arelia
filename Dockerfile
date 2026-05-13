# Copyright (c) 2026 JBT Marel. All rights reserved.

# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for layer-cached restore
COPY Arelia.slnx .
COPY Directory.Build.props .
COPY src/Arelia.Domain/Arelia.Domain.csproj src/Arelia.Domain/
COPY src/Arelia.Application/Arelia.Application.csproj src/Arelia.Application/
COPY src/Arelia.Infrastructure/Arelia.Infrastructure.csproj src/Arelia.Infrastructure/
COPY src/Arelia.Web/Arelia.Web.csproj src/Arelia.Web/

RUN dotnet restore src/Arelia.Web/Arelia.Web.csproj

# Copy source and publish
COPY src/ src/
RUN dotnet publish src/Arelia.Web/Arelia.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# /data must be writable by the app user
RUN mkdir -p /data && chown app:app /data

COPY --from=build --chown=app:app /app/publish .

USER app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

VOLUME ["/data"]

ENTRYPOINT ["dotnet", "Arelia.Web.dll"]
