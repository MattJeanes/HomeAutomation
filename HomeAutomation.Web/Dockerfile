ARG arch=bookworm-slim

FROM mcr.microsoft.com/dotnet/aspnet:9.0-${arch} AS base
RUN apt-get -y update && apt-get -y upgrade && apt-get install -y --no-install-recommends ffmpeg
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0@sha256:6894a71619e08b47ef9df7ff1f436b21d21db160e5d864e180c294a53d7a12f2 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["HomeAutomation.Web/HomeAutomation.Web.csproj", "HomeAutomation.Web/"]
RUN dotnet restore "./HomeAutomation.Web/HomeAutomation.Web.csproj"
COPY . .
WORKDIR "/src/HomeAutomation.Web"
RUN dotnet build "./HomeAutomation.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./HomeAutomation.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HomeAutomation.Web.dll"]