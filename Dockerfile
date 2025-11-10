# Multi-stage build for Video Converter
FROM mcr.microsoft.com/dotnet/sdk:10.0-rc.1-jammy AS build

# Set working directory
WORKDIR /src

# Copy project files
COPY *.sln .
COPY Directory.Build.props .
COPY src/VideoConverter.Api/VideoConverter.Api.csproj src/VideoConverter.Api/
COPY tests/VideoConverter.Tests/VideoConverter.Tests.csproj tests/VideoConverter.Tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build and test
RUN dotnet build -c Release --no-restore
RUN dotnet test -c Release --no-build --verbosity normal

# Publish the application
RUN dotnet publish src/VideoConverter.Api/VideoConverter.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-rc.1-jammy AS runtime

# Install FFmpeg
RUN apt-get update && \
    apt-get install -y ffmpeg && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Set working directory
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

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
ENTRYPOINT ["dotnet", "VideoConverter.Api.dll"]
