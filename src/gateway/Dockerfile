# --- STAGE 1: Build & Publish ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Copy project file and restore dependencies
COPY NexusLogix.Gateway.csproj ./
RUN dotnet restore

# Copy the remaining source files and build
COPY . ./
RUN dotnet publish NexusLogix.Gateway.csproj -c Release -o out

# --- STAGE 2: Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /app/out .

# Expose standard port 80 for proxy traffic
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80

ENTRYPOINT ["dotnet", "NexusLogix.Gateway.dll"]