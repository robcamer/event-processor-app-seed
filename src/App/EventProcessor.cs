using System.Text.Json;
using EFR.NetworkObservability.Common;
using EFR.NetworkObservability.Common.FileWatcher;
using EFR.NetworkObservability.DataModel;
using EFR.NetworkObservability.DataModel.Contexts;
using EFR.NetworkObservability.DataModel.Services;
using EFR.NetworkObservability.RabbitMQ;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static EFR.NetworkObservability.Common.Constants;

namespace EFR.NetworkObservability.EventProcessor;

/// <summary>
/// Processor which is used for processing Event Meta Data
/// </summary>
public class EventProcessor
{
	private readonly PollingFileWatcher fileWatcher;
	private readonly ILogger<EventProcessor> logger;
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
	public EventProcessor(ILogger<EventProcessor> logger, IBus bus, PollingFileWatcher fileWatcher, IDbContextFactory<PcapContext> contextFactory)
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
		logger.LogInformation("Event Meta Data Inserted with ID: {id} ", id);
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
			logger.LogError("Issue publishing event data message to RabbitMQ: {}\n{}", eventdays.ToString(), e);
		}
	}

	/// <summary>
	/// Callback passed to FileWatcher to handle File Created events
	/// </summary>
	/// <param name="filePath">Directory to monitor</param>
	public void OnFileCreated(string filePath)
	{
		if (Utils.FileIsValid(filePath, ext: Common.Enums.FileFilter.Json))
		{
			Process(filePath);
		}

		if ((string.IsNullOrEmpty(filePath) is false) &&
			(File.Exists(filePath) is true))
		{
			File.Delete(filePath);
		}
	}

	private void Process(string filePath)
	{
		try
		{
			string jsonString = File.ReadAllText(filePath);
			EventData eventData = JsonSerializer.Deserialize<EventData>(jsonString)!;

			if (eventData is null)
			{
				logger.LogError($"Unable to deserialize {Path.GetFileName(filePath)} to an EventData object");
			}
			else if (eventData.EventDays is null)
			{
				logger.LogError($"{Path.GetFileName(filePath)} doesn't contain an EventDays object");
			}
			else if (eventData.EventDays.Count == 0)
			{
				logger.LogError($"{Path.GetFileName(filePath)} doesn't contain any EventDay data");
			}
			else
			{
				foreach (EventDays eventdays in eventData.EventDays)
				{
					CreateJob(eventdays);
					PublishMessage(eventdays);
				}
			}
		}
		catch (System.Exception e)
		{
			logger.LogError("Error in Processing EventMetaData {}\n{}", filePath, e);
		}

	}

	/// <summary>
	/// Initiate monitoring on our instance of FileWatcher
	/// </summary>
	public void Monitor()
	{
		fileWatcher.StartPolling(pollingInterval);
	}

	private class EventData
	{
		public IList<EventDays> EventDays
		{
			get; set;
		}
	}

	private class EventDays
	{
		public string JulianDay
		{
			get; set;
		}
		public string Ready
		{
			get; set;
		}
		public string ReProcess
		{
			get; set;
		}
		public string IntervalInSeconds
		{
			get; set;
		}
	}
}