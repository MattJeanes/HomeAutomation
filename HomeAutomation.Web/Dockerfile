FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim@sha256:d5507d8292fb2469ec0b9913f3cd4bb8d5a014bd6dc00c33fd4c4e7310229f07 AS base
RUN apt-get -y update && apt-get -y upgrade && apt-get install -y --no-install-recommends ffmpeg python3 python3-pip
COPY ["HomeAutomation.Web/BoilerGaugeReader/requirements.txt", "/tmp/"]
RUN pip3 install --no-cache-dir -r /tmp/requirements.txt --break-system-packages
RUN apt-get clean && rm -rf /var/lib/apt/lists/*
RUN rm -rf /tmp/requirements.txt
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0@sha256:c5b188baf837b0180a14f988815b9cc7a55b836dd6cbe1b6e6523cf3098faaa8 AS build
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