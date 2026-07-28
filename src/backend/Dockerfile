# --- STAGE 1: Build & Publish ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Copy the project files explicitly to leverage Docker layer caching
COPY Presentation/NexusLogix.WebApi/NexusLogix.WebApi.csproj ./Presentation/NexusLogix.WebApi/
COPY Infrastructure/NexusLogix.Infrastructure/NexusLogix.Infrastructure.csproj ./Infrastructure/NexusLogix.Infrastructure/

# Restore packages by targeting the WebApi project directly
RUN dotnet restore Presentation/NexusLogix.WebApi/NexusLogix.WebApi.csproj

# Copy the rest of the source code files
COPY . ./

# Build and publish the production build
RUN dotnet publish Presentation/NexusLogix.WebApi/NexusLogix.WebApi.csproj -c Release -o out

# --- STAGE 2: Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /app/out .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "NexusLogix.WebApi.dll"]