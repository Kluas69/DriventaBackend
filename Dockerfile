FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /src

# Copy solution and project files
COPY Driventa.slnx .
COPY Driventa.API/ Driventa.API/
COPY src/ src/

# Restore and publish
RUN dotnet restore Driventa.slnx
RUN dotnet publish Driventa.API/Driventa.API.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=builder /app/publish .

# Set ASP.NET Core to listen on PORT environment variable
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}

# Start the application
ENTRYPOINT ["dotnet", "Driventa.API.dll"]
