# STAGE 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Copy the entire solution
COPY . .

# Build the API
RUN dotnet publish src/MyRunshaw.Api/MyRunshaw.Api.csproj -c Release -o /app/api
# Build the Worker
RUN dotnet publish src/MyRunshaw.Workers/MyRunshaw.Workers.csproj -c Release -o /app/worker

# STAGE 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

EXPOSE 8080

COPY --from=build /app/api /app/api
COPY --from=build /app/worker /app/worker

# Run as non-root user
USER $APP_UID