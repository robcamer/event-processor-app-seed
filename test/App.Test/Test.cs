using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EFR.NetworkObservability.Common;
using EFR.NetworkObservability.Common.Enums;
using EFR.NetworkObservability.Common.FileWatcher;
using EFR.NetworkObservability.DataModel.Contexts;
using EFR.NetworkObservability.RabbitMQ;
// using EFR.NetworkObservability.Test;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EFR.NetworkObservability.EventProcessor.Test;

[ExcludeFromCodeCoverage]
public class TestEventProcessor : IDisposable
{
	private readonly DbContextOptions<PcapContext> options =
			new DbContextOptionsBuilder<PcapContext>()
				.UseInMemoryDatabase("TestEventProcessor")
				.Options;

	private string? txtFileName;
	private string eventDataDir;
	private string tmpDir;
	private PcapContext context;
	private EventProcessor eventProcessor;
	private Mock<ILogger<EventProcessor>> mockLogger;
	private Mock<IBus> mockBus;
	private Mock<ISendEndpoint> mockEndpoint;
	private Mock<PollingFileWatcher> mockFileWatcher;
	private Mock<IDbContextFactory<PcapContext>> mockDbFactory;

	public TestEventProcessor()
	{
		tmpDir = Path.GetTempPath();
		eventDataDir = Path.Combine(tmpDir, "eventData");

		Utils.CreateDirectory(eventDataDir, true);

		Environment.SetEnvironmentVariable(Constants.EVENT_META_DATA_DIRECTORY, eventDataDir);
		Environment.SetEnvironmentVariable(Constants.RABBITMQ_HOSTNAME, "localhost");
		Environment.SetEnvironmentVariable(Constants.RABBITMQ_PORT, "5672");
		Environment.SetEnvironmentVariable(Constants.RABBITMQ_USERNAME, "rabbit");
		Environment.SetEnvironmentVariable(Constants.RABBITMQ_PASSWORD, "rabbit");
		Environment.SetEnvironmentVariable(Constants.EVENTDATA_PROCESS_QUEUE, Constants.PCAP_PROCESS_QUEUE);
		Environment.SetEnvironmentVariable(Constants.DB_CONNECTION_STRING, "test");

		mockLogger = new Mock<ILogger<EventProcessor>>();
		mockBus = new Mock<IBus>();
		mockEndpoint = new Mock<ISendEndpoint>();
		mockFileWatcher = new Mock<PollingFileWatcher>(eventDataDir, FileFilter.Json);
		mockDbFactory = new Mock<IDbContextFactory<PcapContext>>();
		context = new PcapContext(options);

		mockBus.Setup(x => x.GetSendEndpoint(It.IsAny<Uri>())).Returns(Task.FromResult(mockEndpoint.Object));
		mockDbFactory.Setup(f => f.CreateDbContext()).Returns(context);

		eventProcessor = new EventProcessor(mockLogger.Object, mockBus.Object, mockFileWatcher.Object, mockDbFactory.Object);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			try
			{
				if (context is not null)
				{
					context.Database.EnsureDeleted();
					context.Dispose();
				}
			}
			catch (Exception) { }
		}
	}

	private static void CreateEmptyFile(string filePath)
	{
		using (File.Create(filePath))
		{
		}
	}

	private void CreateJsonFile()
	{
		string jsonString = @"{""EventDays"" : [
      	  {
          ""JulianDay"": ""123"",
          ""Ready"" : ""true"",
          ""ReProcess"" : ""false"",
          ""IntervalInSeconds"" : ""300""
          },
          {
          ""JulianDay"": ""124"",
          ""Ready"" : ""true"",
          ""ReProcess"" : ""true"",
          ""IntervalInSeconds"" : ""200""
          },
          {
          ""JulianDay"": ""125"",
          ""Ready"" : ""true"",
          ""ReProcess"" : ""false"",
          ""IntervalInSeconds"" : ""100""
          }
  		]
		}";

		WriteJsonFile(jsonString);
	}

	private void CreateJsonBadArrayNameFile()
	{
		string jsonString = @"{""EventData"" : [
      	  {
          ""JulianDay"": ""123"",
          ""Ready"" : ""true"",
          ""ReProcess"" : ""false"",
          ""IntervalInSeconds"" : ""300""
          },
          {
          ""JulianDay"": ""124"",
          ""Ready"" : ""true"",
          ""ReProcess"" : ""true"",
          ""IntervalInSeconds"" : ""200""
          },
          {
          ""JulianDay"": ""125"",
          ""Ready"" : ""true"",
          ""ReProcess"" : ""false"",
          ""IntervalInSeconds"" : ""100""
          }
  		]
		}";

		WriteJsonFile(jsonString);
	}

	private void CreateEmptyJsonNameFile()
	{
		string jsonString = @"{}";

		WriteJsonFile(jsonString);
	}

	private void CreateJsonEmptyArrayFile()
	{
		string jsonString = @"{""EventData"" : [
   		]
		}";

		WriteJsonFile(jsonString);
	}

	private void WriteJsonFile(string jsonString)
	{
		txtFileName = Path.Combine(eventDataDir, "test.json");

		using (FileStream file = File.Create(txtFileName))
		{
			byte[] data = Encoding.UTF8.GetBytes(jsonString);
			file.Write(data, 0, data.Length);
			file.Flush();
			file.Close();
		};
	}

	private void VerifyLog(LogLevel logLevel, string message)
	{
		VerifyLog(logLevel, Times.Once(), message);
	}

	private void VerifyLog(LogLevel logLevel, Times times, string message)
	{
		mockLogger.Verify(
					x => x.Log(
						logLevel,
						It.IsAny<EventId>(),
						It.Is<It.IsAnyType>((o, t) => string.Equals(message, o.ToString(), StringComparison.InvariantCultureIgnoreCase)),
						It.IsAny<Exception>(),
						It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
					times);
	}

	[Fact]
	public void TestInvalidZeroBytesJsonFile()
	{
		txtFileName = Path.Combine(eventDataDir, "test1.json");

		CreateEmptyFile(txtFileName);
		eventProcessor.OnFileCreated(txtFileName);

		Expression<Func<object, Type, bool>> matcher = (object value, Type type) =>
			(value.ToString()!.StartsWith("Error in Processing EventMetaData"));
		TestUtils.VerifyLog(mockLogger, LogLevel.Error, times: Times.Once(), matcher: matcher);

		// //validate event meta data entry
		Assert.True(context.EventMetaDatas.Count() is 0);

		// // validate rabbitmq messages sent
		mockEndpoint.Verify(mock => mock.Send(It.IsAny<EventMetaDataMessage>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public void TestInvalidJsonFile()
	{
		txtFileName = Path.Combine(eventDataDir, "test2.json");
		eventProcessor.OnFileCreated(txtFileName);

		Expression<Func<object, Type, bool>> matcher = (object value, Type type) =>
			(value.ToString()!.StartsWith("Error in Processing EventMetaData"));
		TestUtils.VerifyLog(mockLogger, LogLevel.Error, times: Times.Never(), matcher: matcher);

		// //validate event meta data entry
		Assert.True(context.EventMetaDatas.Count() is 0);

		// // validate rabbitmq messages not sent
		mockEndpoint.Verify(mock => mock.Send(It.IsAny<EventMetaDataMessage>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public void TestOnFileCreatedJsonBadArrayName()
	{
		CreateJsonBadArrayNameFile();
		eventProcessor.OnFileCreated(txtFileName);

		VerifyLog(LogLevel.Error, $"{Path.GetFileName(txtFileName)} doesn't contain an EventDays object");

		//validate event meta data entry
		Assert.True(context.EventMetaDatas.Count() is 0);

		// // validate rabbitmq messages not sent
		mockEndpoint.Verify(mock => mock.Send(It.IsAny<EventMetaDataMessage>(), It.IsAny<CancellationToken>()), Times.Never);

		Assert.True(!File.Exists(txtFileName));
	}


	[Fact]
	public void TestOnFileCreateEmptyJsonNameFile()
	{
		CreateEmptyJsonNameFile();
		eventProcessor.OnFileCreated(txtFileName);

		VerifyLog(LogLevel.Error, $"{Path.GetFileName(txtFileName)} doesn't contain an EventDays object");

		//validate event meta data entry
		Assert.True(context.EventMetaDatas.Count() is 0);

		// // validate rabbitmq messages not sent
		mockEndpoint.Verify(mock => mock.Send(It.IsAny<EventMetaDataMessage>(), It.IsAny<CancellationToken>()), Times.Never);

		Assert.True(!File.Exists(txtFileName));
	}

	[Fact]
	public void TestOnFileCreatedJsonEmptyArray()
	{
		CreateJsonEmptyArrayFile();
		eventProcessor.OnFileCreated(txtFileName);

		VerifyLog(LogLevel.Error, $"{Path.GetFileName(txtFileName)} doesn't contain an EventDays object");

		//validate event meta data entry
		Assert.True(context.EventMetaDatas.Count() is 0);

		// // validate rabbitmq messages not sent
		mockEndpoint.Verify(mock => mock.Send(It.IsAny<EventMetaDataMessage>(), It.IsAny<CancellationToken>()), Times.Never);

		Assert.True(!File.Exists(txtFileName));
	}

	[Fact]
	public void TestOnFileCreated()
	{
		CreateJsonFile();
		eventProcessor.OnFileCreated(txtFileName);

		//validate event meta data entry
		Assert.True(context.EventMetaDatas.Count() is 3);

		// validate rabbitmq messages sent
		mockBus.Verify(mock => mock.GetSendEndpoint(It.IsAny<Uri>()));
		mockEndpoint.Verify(mock => mock.Send(It.IsAny<EventMetaDataMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(3));

		Assert.True(!File.Exists(txtFileName));
	}
}