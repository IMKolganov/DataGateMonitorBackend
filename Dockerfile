# Use the .NET SDK for building
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Set the working directory
WORKDIR /src

# Copy project files for restore. SharedModels comes from NuGet (PackageReference); that package version must be published on nuget.org.
COPY ["DataGateMonitor/DataGateMonitor.csproj", "DataGateMonitor/"]
COPY ["DataGateMonitor.Models/DataGateMonitor.Models.csproj", "DataGateMonitor.Models/"]
COPY ["DataGateMonitor.DataBase/DataGateMonitor.DataBase.csproj", "DataGateMonitor.DataBase/"]
COPY ["DataGateMonitor.Mapping/DataGateMonitor.Mapping.csproj", "DataGateMonitor.Mapping/"]
WORKDIR /src/DataGateMonitor
RUN dotnet restore "DataGateMonitor.csproj"

# Copy the rest of the application source code
WORKDIR /src
COPY . .

# Publish the application (framework-dependent)
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN echo "Using build configuration: $BUILD_CONFIGURATION" && \
    dotnet publish "DataGateMonitor/DataGateMonitor.csproj" \
      -c $BUILD_CONFIGURATION \
      -o /app/publish

# Use the ASP.NET runtime for the final image (framework-dependent)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# Use root initially to allow setting permissions
USER root

WORKDIR /app

# Copy published app
COPY --from=publish /app/publish .

# Copy entrypoint script
COPY entrypoint.sh /entrypoint.sh

# 🔧 Convert CRLF to LF just in case
RUN sed -i 's/\r$//' /entrypoint.sh
RUN sed -i '1s/^\xEF\xBB\xBF//' /entrypoint.sh
RUN chmod +x /entrypoint.sh

# Don't switch to app here — entrypoint.sh will drop privileges
ENTRYPOINT ["/entrypoint.sh"]