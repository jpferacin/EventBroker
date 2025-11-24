using SiriusPt.EventBroker.Core;
using Xunit;

namespace EventBroker.Core.Tests;

public class MultiQueueMetricsTests
{
    [Fact]
    public void MetricsShouldTrackProcessedAndFailed()
    {
        // Arrange
        MultiQueueMetrics.IncrementProcessed("TestQueue");
        MultiQueueMetrics.IncrementFailed("TestQueue", "Handler failed");

        // Act
        var stats = MultiQueueMetrics.QueueStats["TestQueue"];

        // Assert
        Assert.Equal(1, stats.Processed);
        Assert.Equal(1, stats.Failed);
        Assert.True(stats.FailureReasons.ContainsKey("Handler failed"));
    }
}
