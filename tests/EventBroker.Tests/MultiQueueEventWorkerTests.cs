using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SiriusPt.EventBroker.Core;
using SiriusPt.EventBroker.Core.Interfaces;
using SiriusPt.EventBroker.Core.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EventBroker.Core.Tests;

public class MultiQueueEventWorkerTests
{
    [Fact(Skip = "Skipping this test because it fails running in parallel.")]
    public async Task ExecuteAsyncShouldNotProcessMessagesButUpdateMetricsWithFailure()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MultiQueueEventWorker>>();
        var handlerInvokerMock = new Mock<IEventHandlerInvoker>();
        handlerInvokerMock.Setup(x => x.InvokeHandlerAsync(It.IsAny<SubscriberMapping>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(EventProcessResult.Ok());

        var consumerMock = new Mock<IEventConsumer>();
        consumerMock.SetupGet(c => c.QueueName).Returns("TestQueue");
        consumerMock.SetupSequence(c => c.FetchMessageAsync(It.IsAny<CancellationToken>()))
                     .ReturnsAsync("{ \"Id\": \"123\", \"Type\": \"TestEvent\" }")
                     .ReturnsAsync((string?)null);

        var factoryMock = new Mock<IEventConsumerFactory>();
        factoryMock.Setup(f => f.CreateConsumers()).Returns(new List<IEventConsumer> { consumerMock.Object });

        var options = Options.Create(new WorkerPollingOptions());
        var eventConfig = new EventConfiguration();

        var worker = new MultiQueueEventWorker(loggerMock.Object, factoryMock.Object, handlerInvokerMock.Object, eventConfig, options);

        // Act
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await worker.StartAsync(cts.Token);

        var stats = MultiQueueMetrics.QueueStats["TestQueue"];

        // Assert
        handlerInvokerMock.Verify(x => x.InvokeHandlerAsync(It.IsAny<SubscriberMapping>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(0, stats.Processed);
        Assert.Equal(1, stats.Failed);
        Assert.Contains(stats.FailureReasons, k => k.Key.Contains("No handler mapping found") && k.Value == 1);
    }
}
