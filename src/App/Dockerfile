FROM azatecepgdvaacr01.azurecr.us/ironbank/redhat/dotnet-core/dotnetcore-runtime-6.0:6.0 AS base
WORKDIR /app

USER 0

ENV EVENT_META_DATA_DIRECTORY=/var/data/eventdata

RUN mkdir -p $EVENT_META_DATA_DIRECTORY

RUN dnf install -y aspnetcore-runtime-6.0

FROM azatecepgdvaacr01.azurecr.us/ironbank/redhat/dotnet-core/dotnet-sdk-6.0:6.0 AS build
WORKDIR /src

# We need nuget repo username and pwd to restore packages
ARG	GITLAB_PACKAGE_REGISTRY_USERNAME
ARG GITLAB_PACKAGE_REGISTRY_PASSWORD

COPY . .

USER root

WORKDIR "/src/EventProcessor"
RUN dotnet build "EventProcessor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EventProcessor.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY EventProcessor/docker-healthcheck.sh /app/
COPY --from=publish /app/publish .

HEALTHCHECK CMD ["/app/docker-healthcheck.sh"]
ENTRYPOINT ["dotnet", "EventProcessor.dll"]
