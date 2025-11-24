using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SiriusPt.EventBroker.Core;
using SiriusPt.EventBroker.Core.Interfaces;
using SiriusPt.EventBroker.Core.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EventBroker.Core.Tests;


public class EventHandlerInvokerTests
{
    // Test classes
    public class TestEvent
    {
        public string Id { get; set; } = string.Empty;
    }

    public class TestHandler : IEventHandler<TestEvent>
    {
        public Task<EventProcessResult> HandleAsync(TestEvent request, CancellationToken cancellationToken)
        {
            return Task.FromResult(EventProcessResult.Ok());
        }
    }

    public class FailingHandler : IEventHandler<TestEvent>
    {
        public Task<EventProcessResult> HandleAsync(TestEvent request, CancellationToken cancellationToken)
        {
            throw new Exception("Handler failed");
        }
    }

    [Fact]
    public async Task InvokeHandlerAsyncShouldReturnOkWhenHandlerSucceeds()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EventHandlerInvoker>>();
        var serviceCollection = new ServiceCollection().AddScoped<TestHandler>();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var handlerInvoker = new EventHandlerInvoker(serviceProvider, loggerMock.Object);
        var mapping = SubscriberMapping.Create<TestHandler, TestEvent>();

        // Act
        var result = await handlerInvoker.InvokeHandlerAsync(mapping, new TestEvent { Id = "123" }, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public async Task InvokeHandlerAsyncShouldReturnFailWhenNoHandlerFound()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EventHandlerInvoker>>();
        var serviceCollection = new ServiceCollection().AddScoped<TestHandler>();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var handlerInvoker = new EventHandlerInvoker(serviceProvider, loggerMock.Object);
        var mapping = SubscriberMapping.Create<FailingHandler, TestEvent>();

        // Act
        var result = await handlerInvoker.InvokeHandlerAsync(mapping, new TestEvent { Id = "123" }, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task InvokeHandlerAsyncShouldThrowWhenHandlerNotFound()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EventHandlerInvoker>>();
        var serviceCollection = new ServiceCollection().AddScoped<TestHandler>();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var handlerInvoker = new EventHandlerInvoker(serviceProvider, loggerMock.Object);
        var mapping = SubscriberMapping.Create<FailingHandler, TestEvent>();

        // Act
        var result = await handlerInvoker.InvokeHandlerAsync(mapping, new { Id = "123" }, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
    }


}
