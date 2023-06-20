using System.Text.Json;
using EFR.NetworkObservability.Common;
using EFR.NetworkObservability.Common.FileWatcher;
using EFR.NetworkObservability.DataModel;
using EFR.NetworkObservability.DataModel.Contexts;
using EFR.NetworkObservability.DataModel.Services;
using EFR.NetworkObservability.RabbitMQ;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;
using static EFR.NetworkObservability.Common.Constants;

namespace EFR.NetworkObservability.EventProcessor;

/// <summary>
/// Processor which is used for processing Event Meta Data
/// </summary>
public class EventProcessor
{
  private readonly PollingFileWatcher fileWatcher;
  private readonly ILogger logger;
  private readonly IBus bus;
  private readonly IDbContextFactory<PcapContext> contextFactory;
  private readonly string eventDataDir;
  private readonly string eventProcessQueue;
  private readonly long pollingInterval;

  /// <summary>
  /// Instantiates the EventProcessor with required parameters
  /// </summary>
  /// <param name="logger">The logger instance</param>
  /// <param name="bus">The logger instance</param>
  /// <param name="fileWatcher">Class which uses .NET FileSystemWatcher to monitor for new files.</param>
  /// <param name="contextFactory">Database context scope factory</param>
  public EventProcessor(ILogger logger, IBus bus, PollingFileWatcher fileWatcher, IDbContextFactory<PcapContext> contextFactory)
  {

    this.logger = logger;
    this.bus = bus;
    this.fileWatcher = fileWatcher;
    this.contextFactory = contextFactory;

    eventDataDir = Utils.GetEnvVar(EVENT_META_DATA_DIRECTORY);
    eventProcessQueue = Utils.GetEnvVar(EVENTDATA_PROCESS_QUEUE);
    pollingInterval = Utils.GetEnvVarOrDefault<long>(FILE_POLLING_INTERVAL, 30000);

    Utils.CreateDirectory(eventDataDir);

    this.fileWatcher.OnFileCreated += OnFileCreated;
  }

  private async void CreateJob(EventDays eventdays)
  {
    PcapContext context = contextFactory.CreateDbContext();
    PcapService service = new(context);
    EventMetaData eventMetaData = new()
    {
      JulianDay = int.Parse(eventdays.JulianDay),
      Ready = bool.Parse(eventdays.Ready),
      Reprocess = bool.Parse(eventdays.ReProcess),
      IntervalInSeconds = int.Parse(eventdays.IntervalInSeconds)
    };

    long id = await service.InsertEventMetaDataAsync(eventMetaData);
    logger.Information($"Event Meta Data Inserted with ID: {id}!");
  }

  private async void PublishMessage(EventDays eventdays)
  {

    EventMetaDataMessage message = new()
    {
      JulianDay = eventdays.JulianDay.ToString(),
      Ready = eventdays.Ready.ToString(),
      ReProcess = eventdays.ReProcess.ToString(),
      IntervalInSeconds = eventdays.IntervalInSeconds.ToString()
    };

    try
    {
      var endpoint = await bus.GetSendEndpoint(new Uri(string.Format("{0}:{1}", RABBITMQ_QUEUE, eventProcessQueue)));
      await endpoint.Send<EventMetaDataMessage>(message);
    }
    catch (Exception e)
    {
      logger.Error(e, $"Issue publishing event data message to RabbitMQ: {eventdays}!");
    }
  }

  /// <summary>
  /// Callback passed to FileWatcher to handle File Created events
  /// </summary>
  /// <param name="filePath">Directory to monitor</param>
  public void OnFileCreated(string filePath)
  {
    if (string.IsNullOrEmpty(filePath) is true)
    {
      logger.Error($"Provided filepath is null!");
      return;
    }

    var fileInfo = new FileInfo(filePath);

    if (fileInfo.Exists is false)
    {
      logger.Error($"File: {filePath} does not exists!");
      return;
    }

    if (fileInfo.Length is 0)
    {
      logger.Error($"File: {filePath} is empty!");
      return;
    }

    if (Utils.FileAvailable(filePath))
    {
      Process(filePath);
    }
  }

  private void Process(string filePath)
  {
    try
    {
      string jsonString = File.ReadAllText(filePath);
      EventData eventData = JsonSerializer.Deserialize<EventData>(jsonString);

      if (eventData is null)
      {
        logger.Error($"Unable to deserialize {Path.GetFileName(filePath)} to an EventData object!");
      }
      else if (eventData.EventDays is null)
      {
        logger.Error($"{Path.GetFileName(filePath)} doesn't contain an EventDays object!");
      }
      else if (eventData.EventDays.Count == 0)
      {
        logger.Error($"{Path.GetFileName(filePath)} doesn't contain any EventDay data!");
      }
      else
      {
        foreach (EventDays eventdays in eventData.EventDays)
        {
          CreateJob(eventdays);
          PublishMessage(eventdays);
        }
      }

      File.Delete(filePath);
    }
    catch (Exception e)
    {
      logger.Error(e, $"Error in Processing EventMetaData {filePath}!", filePath);
    }

  }

  /// <summary>
  /// Initiate monitoring on our instance of FileWatcher
  /// </summary>
  public void Monitor()
  {
    fileWatcher.StartPolling(pollingInterval);
  }

  private record EventData(IList<EventDays> EventDays);

  private record EventDays(string JulianDay, string Ready, string ReProcess, string IntervalInSeconds);
}
