FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS base
RUN apt-get -y update && apt-get -y upgrade && apt-get install -y --no-install-recommends ffmpeg python3 python3-pip
COPY ["HomeAutomation.Web/BoilerGaugeReader/requirements.txt", "/tmp/"]
RUN pip3 install --no-cache-dir -r /tmp/requirements.txt --break-system-packages
RUN apt-get clean && rm -rf /var/lib/apt/lists/*
RUN rm -rf /tmp/requirements.txt
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0@sha256:d7f4691d11f610d9b94bb75517c9e78ac5799447b5b3e82af9e4625d8c8d1d53 AS build
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