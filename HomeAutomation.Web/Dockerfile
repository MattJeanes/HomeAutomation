ARG arch=bookworm-slim

FROM mcr.microsoft.com/dotnet/aspnet:9.0-${arch} AS base
RUN apt-get -y update && apt-get -y upgrade && apt-get install -y --no-install-recommends ffmpeg python3 python3-pip
COPY ["HomeAutomation.Web/BoilerGaugeReader/requirements.txt", "/tmp/"]
RUN pip3 install --no-cache-dir -r /tmp/requirements.txt --break-system-packages
RUN apt-get clean && rm -rf /var/lib/apt/lists/*
RUN rm -rf /tmp/requirements.txt
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0@sha256:1025bed126a7b85c56b960215ab42a99db97a319a72b5d902383ebf6c6e62bbe AS build
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