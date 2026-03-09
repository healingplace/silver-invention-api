# Multi-stage build for ASP.NET Core API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /build

# Copy project files
COPY DocumentUploaderAPI/DocumentUploaderAPI.csproj ./
RUN dotnet restore

# Copy source code
COPY DocumentUploaderAPI/ ./

# Build the application
RUN dotnet build -c Release -o /build/output

# Publish the application
RUN dotnet publish -c Release -o /publish


# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app

# Install curl for health checks
RUN apk add --no-cache curl

# Copy published application from build stage
COPY --from=build-env /publish .

# Create a non-root user for security
RUN addgroup -g 1001 nonroot && \
    adduser -u 1001 -G nonroot -s /bin/sh -D nonroot && \
    chown -R nonroot:nonroot /app

USER nonroot

# Expose ports
EXPOSE 8080 8443

# Set environment for production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "DocumentUploaderAPI.dll"]
