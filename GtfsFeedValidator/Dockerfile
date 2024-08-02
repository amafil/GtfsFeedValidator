FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y default-jre
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GtfsFeedValidator.csproj", "."]
RUN dotnet restore "./GtfsFeedValidator.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./GtfsFeedValidator.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GtfsFeedValidator.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN wget https://github.com/MobilityData/gtfs-validator/releases/download/v5.0.1/gtfs-validator-5.0.1-cli.jar -O gtfs-validator.5.0.1-cli.jar
ENTRYPOINT ["dotnet", "GtfsFeedValidator.dll"]