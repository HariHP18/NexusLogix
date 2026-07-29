FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy all .csproj files preserving directory structure for cached restoration
COPY Core/NexusLogix.Domain/NexusLogix.Domain.csproj Core/NexusLogix.Domain/
COPY Core/NexusLogix.Application/NexusLogix.Application.csproj Core/NexusLogix.Application/
COPY Infrastructure/NexusLogix.Infrastructure/NexusLogix.Infrastructure.csproj Infrastructure/NexusLogix.Infrastructure/
COPY Presentation/NexusLogix.WebApi/NexusLogix.WebApi.csproj Presentation/NexusLogix.WebApi/

# Restore dependencies
RUN dotnet restore Presentation/NexusLogix.WebApi/NexusLogix.WebApi.csproj

# Copy the remaining backend source code and build
COPY . .
WORKDIR /src/Presentation/NexusLogix.WebApi
RUN dotnet build NexusLogix.WebApi.csproj -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish NexusLogix.WebApi.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NexusLogix.WebApi.dll"]