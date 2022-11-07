using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Moq;

namespace EFR.NetworkObservability.EventProcessor.Test;

[ExcludeFromCodeCoverage]
public static class TestUtils
{
	public static void VerifyLog<T>(Mock<ILogger<T>> mockLogger, LogLevel logLevel, string? message = null, Expression<Func<object, Type, bool>>? matcher = null)
	{
		VerifyLog(mockLogger, logLevel, Times.Once(), message, matcher);
	}

	public static void VerifyLog<T>(Mock<ILogger<T>> mockLogger, LogLevel logLevel, Times times, string? message = null, Expression<Func<object, Type, bool>>? matcher = null)
	{
		if (matcher == null)
		{
			matcher = (value, type) => string.Equals(message, value.ToString(), StringComparison.InvariantCultureIgnoreCase);
		}

		mockLogger.Verify(
					x => x.Log(
						logLevel,
						It.IsAny<EventId>(),
						It.Is<It.IsAnyType>(matcher),
						It.IsAny<Exception>(),
						It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
					times);
	}
}