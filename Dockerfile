FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

ARG GITHUB_PACKAGE_REGISTRY_USERNAME
ARG GITHUB_PACKAGE_REGISTRY_PASSWORD
ARG NUGET_SOURCE_URL
ARG NUGET_PLATFORM_URL

WORKDIR /src
COPY src/App/EventProcessor.csproj .
COPY src/App/nuget.config .
RUN dotnet restore

COPY src/App .
RUN dotnet publish -c Release -o /app --no-restore

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0 as app
WORKDIR /app
ENV EVENT_META_DATA_DIRECTORY=/var/data/eventdata

RUN mkdir -p $EVENT_META_DATA_DIRECTORY

COPY /src/App/docker-healthcheck.sh /app/
COPY --from=build /app .

HEALTHCHECK CMD ["/app/docker-healthcheck.sh"]
ENTRYPOINT ["dotnet", "EventProcessor.dll"]