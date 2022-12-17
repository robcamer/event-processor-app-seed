using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;

namespace EFR.NetworkObservability.EventProcessor;

/// <summary>
/// Main class for EventProcessor
/// </summary>
[ExcludeFromCodeCoverage]
public class EventProcessorService : BackgroundService
{
  private readonly EventProcessor processor;

  /// <summary>
  /// Instantiates the EventProcessorService with required parameters
  /// </summary>
  /// <param name="processor">EventProcessor</param>
  public EventProcessorService(EventProcessor processor)
  {
    this.processor = processor;
  }

  /// <summary>
  /// Setup the file watcher and keep running until told to stop
  /// </summary>
  /// <param name="stoppingToken">Indicates its time to stop</param>
  /// <returns>Task</returns>
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    processor.Monitor();

    await Task.Yield();

    stoppingToken.WaitHandle.WaitOne();
  }
}
