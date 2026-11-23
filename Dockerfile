# syntax=docker/dockerfile:1

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the files needed to restore, so this layer is cached independently of source changes.
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/StoreApi.Domain/StoreApi.Domain.csproj", "src/StoreApi.Domain/"]
COPY ["src/StoreApi.Application/StoreApi.Application.csproj", "src/StoreApi.Application/"]
COPY ["src/StoreApi.Infrastructure/StoreApi.Infrastructure.csproj", "src/StoreApi.Infrastructure/"]
COPY ["src/StoreApi.Api/StoreApi.Api.csproj", "src/StoreApi.Api/"]

# Restore the API project graph only (test projects are excluded from the image build context).
RUN dotnet restore "src/StoreApi.Api/StoreApi.Api.csproj"

COPY src/ src/

RUN dotnet publish "src/StoreApi.Api/StoreApi.Api.csproj" \
        --configuration Release \
        --no-restore \
        --output /app/publish \
        /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for the container health check, then drop privileges.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && addgroup --system --gid 1001 appgroup \
    && adduser --system --ingroup appgroup --uid 1001 appuser

COPY --from=build /app/publish .

# The publish output inherits restrictive modes from the build context; make it readable
# by the unprivileged runtime user.
RUN chmod -R a+rX /app && \
    mkdir -p /app/logs && \
    chown -R appuser:appgroup /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=5 \
  CMD curl --fail --silent http://localhost:8080/health || exit 1

USER appuser

ENTRYPOINT ["dotnet", "StoreApi.Api.dll"]
