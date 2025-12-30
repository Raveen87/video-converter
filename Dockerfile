# Multi-stage build for Video Converter
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Install Native AOT prerequisites
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       clang \
       zlib1g-dev \
       python3 \
    && rm -rf /var/lib/apt/lists/*

# Install WASM tools for Blazor AOT
RUN dotnet workload install wasm-tools

WORKDIR /src

# Copy solution and project files
COPY *.sln .
COPY Directory.Build.props .
COPY src/VideoConverter.Api/VideoConverter.Api.csproj src/VideoConverter.Api/
COPY src/VideoConverter.Common/VideoConverter.Common.csproj src/VideoConverter.Common/
COPY src/VideoConverter.Web/VideoConverter.Web.csproj src/VideoConverter.Web/
COPY tests/VideoConverter.Tests/VideoConverter.Tests.csproj tests/VideoConverter.Tests/
COPY tests/VideoConverter.Web.Tests/VideoConverter.Web.Tests.csproj tests/VideoConverter.Web.Tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build and test
RUN dotnet build -c Release --no-restore
RUN dotnet test -c Release --no-build --verbosity normal

# Publish Web (Blazor WASM AOT)
WORKDIR /src/src/VideoConverter.Web
RUN dotnet publish -c Release -o /app/publish/web

# Publish API (Native AOT)
WORKDIR /src/src/VideoConverter.Api
# Self-contained is implied by PublishAot=true, but specifying linux-x64 ensures correct target
RUN dotnet publish -c Release -r linux-x64 -o /app/publish/api

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble AS runtime

# Install FFmpeg
RUN apt-get update && \
    apt-get install -y --no-install-recommends ffmpeg && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

WORKDIR /app

# Copy published artifacts
COPY --from=build /app/publish/api/VideoConverter.Api .
COPY --from=build /app/publish/web/wwwroot ./wwwroot

# Create directories for data and temp files
RUN mkdir -p /app/data /app/temp && \
    chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Start the application
ENTRYPOINT ["./VideoConverter.Api"]
