using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SiriusPt.EventBroker.Core;
using SiriusPt.EventBroker.Core.Interfaces;
using System.Threading;
using Xunit;
using Moq;

namespace EventBroker.Core.Tests;

public class EventQueueWorkerTests
{
    [Fact]
    public async Task DispatchRawEventAsync_ShouldReturnFailForInvalidJson()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EventQueueWorker>>();
        var handlerInvokerMock = new Mock<IEventHandlerInvoker>();
        var consumerMock = new Mock<IEventConsumer>();
        consumerMock.SetupGet(c => c.QueueName).Returns("TestQueue");
        var pollingOptions = new WorkerPollingOptions();
        var eventConfig = new EventConfiguration();
        var worker = new TestEventQueueWorker(loggerMock.Object, consumerMock.Object, handlerInvokerMock.Object, eventConfig, pollingOptions);

        // Act
        var result = await worker.DispatchRawEventAsync("TestEvent", "invalid-json", CancellationToken.None);

        // Assert
        Assert.False(result.Success);
    }

    private class TestEventQueueWorker : EventQueueWorker
    {
        public TestEventQueueWorker(ILogger<EventQueueWorker> logger, IEventConsumer consumer, IEventHandlerInvoker invoker, EventConfiguration config, WorkerPollingOptions options)
            : base(logger, consumer, invoker, config, options) { }
    }
}
