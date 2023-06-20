using Moq;
using Serilog;

namespace EFR.NetworkObservability.TestEventProcessor.Test
{
  internal static class CustomLogVerify
  {
    public static void ShouldLogError(this Mock<ILogger> mockLogger, string message)
    {
      mockLogger.Verify(logger => logger.Error(It.Is<string>(s => s.Contains(message))), Times.AtLeastOnce);
    }

    public static void ShouldLogInfo(this Mock<ILogger> mockLogger, string message)
    {
      mockLogger.Verify(logger => logger.Information(It.Is<string>(s => s.Contains(message))), Times.AtLeastOnce);
    }
  }
}
