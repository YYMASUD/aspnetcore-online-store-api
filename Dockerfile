# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["StoreApi.sln", "./"]
COPY ["src/StoreApi.Domain/StoreApi.Domain.csproj", "src/StoreApi.Domain/"]
COPY ["src/StoreApi.Application/StoreApi.Application.csproj", "src/StoreApi.Application/"]
COPY ["src/StoreApi.Infrastructure/StoreApi.Infrastructure.csproj", "src/StoreApi.Infrastructure/"]
COPY ["src/StoreApi.Api/StoreApi.Api.csproj", "src/StoreApi.Api/"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build and publish
WORKDIR /src/src/StoreApi.Api
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user
RUN addgroup --system --gid 1001 appgroup && \
    adduser --system --ingroup appgroup --uid 1001 appuser

# Copy published app
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl --fail http://localhost:80/health || exit 1

# Switch to non-root user
USER appuser

# Run the application
ENTRYPOINT ["dotnet", "StoreApi.Api.dll"]
