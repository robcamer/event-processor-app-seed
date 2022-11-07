# TODO - Seed Creation Steps

This is the overview list of steps when creating an app seed. All places that require modification has been marked with `TODO`. Here are the steps:

1. Use this repo as a template in the new seed repo creation
2. In project root directory, run `grep -nr "TODO" ./` to preview all files that require action
3. Update projects to use the specific project info instead of placeholder, general items are
   1. Update pipeline files `.gitlab-ci.yml` and `/github/github.action.yml` to build, test, and push on both platforms
4. Run step 2 again to ensure no more updates are left
5. Remove this top section when done

# EventProcessor App Seed

This is an application seed that is part of the general Network Observability solution consisting of containerized .NET application development with cloud-native capabilities.

Follow this link for a more generic version of a [.NET Project App Seed](https://github.com/microsoft/coral-seed-dotnet-core-webapi)

Backlog in Coral project: [#170 Create new coral-seed-dotnet-core-webapi on .NET Core 6.0](https://github.com/microsoft/coral/issues/170)

## Getting Started

To get started with Coral, see the [platform setup instructions](https://github.com/microsoft/coral/blob/main/docs/platform-setup.md) in the main [Coral](https://github.com/microsoft/coral) repo.

## Baseline Features

The application seed contains the following cloud-native capabilities:

1. Performance metrics
2. Data storage
3. Metadata for observability

## Network Observability Features

EventProcessor will process an event metadata json and store it in the Database for Observability dashboards

Input:

an event json, sample can be found in [eventsample.json](eventsample.json)

Output:

![Saved event](docs/db.png)

### Application Development

After secret file `.env` has been configured, load it with `export 'cat .env' && bash`

- In src/App, run `dotnet build` to build your app
- In src/App, run `dotnet run` to run your app
- In test/App.Test, run `dotnet test` to run unit tests

### Container Development

Your Codespace has a preconfigured running cluster so that you can validate how your application will behave when deployed

- From `Tasks: Run Task` (press `Ctrl+Shift+P`), select `Run in local cluster`. Your app will load in a new browser tab.
- To reset the local cluster for any reason, you can use the `Reset local cluster` task

Your Codespace also has a local portal that exposes a number of services that will also be present when your app is deployed

- Using `Tasks: Run Task`, select `Launch Platform Portal in New Tab`
- The application increments a metric named `hits` each time the "Hello, World!" path is used. From the portal, use [Prometheus](https://prometheus.io/) to query app metrics
- [Grafana](https://grafana.com/) is also available for visualization. The instance is local to your Codespace with the following settings:
  - A `Sample Dashboard` to see the `api_hits_total` counter (Navigate to the 'General' folder to see the link)
- [Fluentbit](https://fluentbit.io/) is configured to scrape the application and Kubernetes logs and can be accessed from the portal. A Fluentbit filter is applied to the application logs to exclude log messages that contain 200 responses at the `/healthcheck` and `/metrics` endpoints. [Serilog](https://serilog.net/) can be configured to your needs. For more information on how to configure the logger, see the [Serilog wiki](https://github.com/serilog/serilog/wiki/Configuration-Basics).

> You can also view the list of [forwarded ports](https://docs.github.com/en/codespaces/developing-in-codespaces/forwarding-ports-in-your-codespace) available to your Codespace in the `PORTS` tab in the Integrated Terminal

### Configuring a different Container Registry

This environment comes with github actions to build and push the application containers to the GitHub container registry.

If you wish, you can configure the repo to use the container registry of your choice by [creating the following secrets in the repository](https://docs.github.com/en/actions/reference/encrypted-secrets#creating-encrypted-secrets-for-a-repository):

- CONTAINER_REGISTRY_URL
- CONTAINER_REGISTRY_USER
- CONTAINER_REGISTRY_ACCESS_TOKEN

These values can be obtained from your specific container registry provider.

> **Note**
>
> To return to the default, simply remove the secrets from the repository.

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft
trademarks or logos is subject to and must follow
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
