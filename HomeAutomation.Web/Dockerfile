FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim@sha256:22bcafd1f3c3bec34af587d205c631380352b15eeb52f9376f7cb3f6489c1f6f AS base
RUN apt-get -y update && apt-get -y upgrade && apt-get install -y --no-install-recommends ffmpeg python3 python3-pip
COPY ["HomeAutomation.Web/BoilerGaugeReader/requirements.txt", "/tmp/"]
RUN pip3 install --no-cache-dir -r /tmp/requirements.txt --break-system-packages
RUN apt-get clean && rm -rf /var/lib/apt/lists/*
RUN rm -rf /tmp/requirements.txt
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0@sha256:90872f8e7f1fd2b93989b81fb7f152c3bef4fe817470a3227abaa18c873dba60 AS build
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