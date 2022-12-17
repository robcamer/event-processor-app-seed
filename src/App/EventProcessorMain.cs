using System;
using System.Diagnostics.CodeAnalysis;
using EFR.NetworkObservability.Common;
using EFR.NetworkObservability.Common.FileWatcher;
using EFR.NetworkObservability.DataModel.Contexts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EFR.NetworkObservability.EventProcessor;

/// <summary>
/// Main class for EventProcessor
/// </summary>
[ExcludeFromCodeCoverage]
public class EventProcessorMain
{

  /// <summary>
  /// Host Builder api
  /// </summary>
  public static IHost CreateHost(string[] args)
    => Host.CreateDefaultBuilder(args)
        .ConfigureServices((hostContext, services) =>
        {
          string eventMetaDataDir = Utils.GetEnvVar(Constants.EVENT_META_DATA_DIRECTORY);
          string dbConnectionString = Utils.GetEnvVar(Constants.DB_CONNECTION_STRING);
          ushort rabbitMQPort = Utils.GetEnvVarOrDefault<ushort>(Constants.RABBITMQ_PORT, 5672);
          string rabbitMQHostname = Utils.GetEnvVar(Constants.RABBITMQ_HOSTNAME);
          string rabbitMQUsername = Utils.GetEnvVar(Constants.RABBITMQ_USERNAME);
          string rabbitMQPassword = Utils.GetEnvVar(Constants.RABBITMQ_PASSWORD);

          services.AddDbContextFactory<PcapContext>(options => options.UseSqlServer(dbConnectionString));
          services.AddSingleton(_ => new PollingFileWatcher(eventMetaDataDir, "*.*"));
          services.AddSingleton<EventProcessor>();
          services.AddHostedService<EventProcessorService>();

          services.AddMassTransit(mtConfig =>
          {
            mtConfig.UsingRabbitMq((context, rabbitConfig) =>
            {
              rabbitConfig.Host(rabbitMQHostname, rabbitMQPort, "/", hostConfig =>
              {
                hostConfig.Username(rabbitMQUsername);
                hostConfig.Password(rabbitMQPassword);
              });
            });
          });
        }).Build();

  /// <summary>
  /// Main api
  /// </summary>
  public static async Task Main(string[] args) => await CreateHost(args).RunAsync();

}
