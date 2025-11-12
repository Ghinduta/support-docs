# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY src/StackOverflowRAG.Api/StackOverflowRAG.Api.csproj src/StackOverflowRAG.Api/
COPY src/StackOverflowRAG.Core/StackOverflowRAG.Core.csproj src/StackOverflowRAG.Core/
COPY src/StackOverflowRAG.Data/StackOverflowRAG.Data.csproj src/StackOverflowRAG.Data/
COPY src/StackOverflowRAG.ML/StackOverflowRAG.ML.csproj src/StackOverflowRAG.ML/

# Restore dependencies (only for Api project and its dependencies)
RUN dotnet restore src/StackOverflowRAG.Api/StackOverflowRAG.Api.csproj

# Copy all source code
COPY src/ src/

# Build and publish
WORKDIR /src/src/StackOverflowRAG.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Set environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

# Start the application
ENTRYPOINT ["dotnet", "StackOverflowRAG.Api.dll"]
