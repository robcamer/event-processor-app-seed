using System.Diagnostics.CodeAnalysis;
using System.Text;
using EFR.NetworkObservability.Common;
using EFR.NetworkObservability.Common.FileWatcher;
using EFR.NetworkObservability.DataModel.Contexts;
using EFR.NetworkObservability.RabbitMQ;
using EFR.NetworkObservability.TestEventProcessor.Test;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Moq;
using Serilog;
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
  private readonly string eventDataDir;
  private readonly string tmpDir;
  private readonly PcapContext context;
  private readonly EventProcessor eventProcessor;
  private readonly Mock<ILogger> mockLogger;
  private readonly Mock<IBus> mockBus;
  private readonly Mock<ISendEndpoint> mockEndpoint;
  private readonly Mock<PollingFileWatcher> mockFileWatcher;
  private readonly Mock<IDbContextFactory<PcapContext>> mockDbFactory;

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

    mockLogger = new Mock<ILogger>();
    mockBus = new Mock<IBus>();
    mockEndpoint = new Mock<ISendEndpoint>();
    mockFileWatcher = new Mock<PollingFileWatcher>(eventDataDir, "*.*");
    mockDbFactory = new Mock<IDbContextFactory<PcapContext>>();
    context = new PcapContext(options);

    mockBus.Setup(x => x.GetSendEndpoint(It.IsAny<Uri>())).Returns(Task.FromResult(mockEndpoint.Object));
    mockDbFactory.Setup(f => f.CreateDbContext()).Returns(context);

    eventProcessor = new EventProcessor(mockLogger.Object, mockBus.Object, mockFileWatcher.Object, mockDbFactory.Object);
  }

  public void Dispose()
  {
    context.Database.EnsureDeleted();
    context.Dispose();

    GC.SuppressFinalize(this);
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

  [Fact]
  public void TestInvalidZeroBytesJsonFile()
  {
    txtFileName = Path.Combine(eventDataDir, "test1.json");

    File.Create(txtFileName).Dispose();

    eventProcessor.OnFileCreated(txtFileName);

    mockLogger.ShouldLogError($"File: {txtFileName} is empty!");

    Assert.True(context.EventMetaDatas.Count() is 0);

    mockEndpoint.Verify(mock => mock.Send(It.IsAny<EventMetaDataMessage>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public void TestInvalidJsonFile()
  {
    txtFileName = Path.Combine(eventDataDir, "test2.json");

    eventProcessor.OnFileCreated(txtFileName);

    mockLogger.ShouldLogError($"File: {txtFileName} does not exists!");

    Assert.True(context.EventMetaDatas.Count() is 0);

    mockEndpoint.Verify(mock => mock.Send(It.IsAny<EventMetaDataMessage>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public void TestOnFileCreatedJsonBadArrayName()
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

    eventProcessor.OnFileCreated(txtFileName);

    mockLogger.ShouldLogError($"{Path.GetFileName(txtFileName)} doesn't contain an EventDays object");

    Assert.True(context.EventMetaDatas.Count() is 0);

    mockEndpoint.Verify(mock => mock.Send(It.IsAny<EventMetaDataMessage>(), It.IsAny<CancellationToken>()), Times.Never);

    Assert.True(!File.Exists(txtFileName));
  }

  [Fact]
  public void TestOnFileCreateEmptyJsonNameFile()
  {
    string jsonString = @"{}";

    WriteJsonFile(jsonString);

    eventProcessor.OnFileCreated(txtFileName);

    mockLogger.ShouldLogError($"{Path.GetFileName(txtFileName)} doesn't contain an EventDays object");

    Assert.True(context.EventMetaDatas.Count() is 0);

    mockEndpoint.Verify(mock => mock.Send(It.IsAny<EventMetaDataMessage>(), It.IsAny<CancellationToken>()), Times.Never);

    Assert.True(!File.Exists(txtFileName));
  }

  [Fact]
  public void TestOnFileCreatedJsonEmptyArray()
  {
    string jsonString = @"{""EventData"" : [
   		]
		}";

    WriteJsonFile(jsonString);

    eventProcessor.OnFileCreated(txtFileName);

    mockLogger.ShouldLogError($"{Path.GetFileName(txtFileName)} doesn't contain an EventDays object");


    Assert.True(context.EventMetaDatas.Count() is 0);

    mockEndpoint.Verify(mock => mock.Send(It.IsAny<EventMetaDataMessage>(), It.IsAny<CancellationToken>()), Times.Never);

    Assert.True(!File.Exists(txtFileName));
  }

  [Fact]
  public void TestOnFileCreated()
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

    eventProcessor.OnFileCreated(txtFileName);

    //validate event meta data entry
    Assert.True(context.EventMetaDatas.Count() is 3);

    // validate rabbitmq messages sent
    mockBus.Verify(mock => mock.GetSendEndpoint(It.IsAny<Uri>()));
    mockEndpoint.Verify(mock => mock.Send(It.IsAny<EventMetaDataMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(3));

    Assert.True(!File.Exists(txtFileName));
  }
}
