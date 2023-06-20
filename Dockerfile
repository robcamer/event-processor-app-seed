FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

ARG GITHUB_PACKAGE_REGISTRY_USERNAME
ARG GITHUB_PACKAGE_REGISTRY_PASSWORD
ARG NUGET_SOURCE_URL
ARG NUGET_PLATFORM_URL

# Copy necessary files
COPY ./src/App/ ./
COPY nuget.config ./

# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out --no-restore

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0 as app
WORKDIR /app

RUN apt-get update -y

ENV EVENT_META_DATA_DIRECTORY=/var/data/eventdata

RUN mkdir -p $EVENT_META_DATA_DIRECTORY

COPY ./src/App/docker-healthcheck.sh .
COPY --from=build-env /app/out .

HEALTHCHECK CMD ["/app/docker-healthcheck.sh"]
ENTRYPOINT ["dotnet", "EventProcessor.dll"]