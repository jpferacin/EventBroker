using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Amazon.SQS;
using SiriusPt.EventBroker.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Moq;

namespace EventBroker.Transport.SQS.Tests;

public class SqsEventConsumerFactoryTests
{
    [Fact]
    public void CreateConsumerWithQueueOptionsShouldReturnValidConsumer()
    {
        // Arrange
        var sqsClientMock = new Mock<IAmazonSQS>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(l => l.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

        var queueOptions = new QueueOptions { Name = "Workday", Url = "https://sqs.test/Workday", DLQUrl = "https://sqs.test/Workday-DLQ" };
        var factory = new SqsEventConsumerFactory(sqsClientMock.Object, loggerFactoryMock.Object, Options.Create(new QueueConfiguration { Queues = new List<QueueOptions> { queueOptions } }));

        // Act
        var consumer = factory.CreateConsumer(queueOptions);

        // Assert
        Assert.NotNull(consumer);
        Assert.Equal("Workday", consumer.QueueName);
    }

    [Fact]
    public void CreateConsumerWithValidQueueNameShouldReturnConsumer()
    {
        // Arrange
        var sqsClientMock = new Mock<IAmazonSQS>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(l => l.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

        var queueOptions = new QueueOptions { Name = "Workday", Url = "https://sqs.test/Workday", DLQUrl = "https://sqs.test/Workday-DLQ" };
        var factory = new SqsEventConsumerFactory(sqsClientMock.Object, loggerFactoryMock.Object, Options.Create(new QueueConfiguration { Queues = new List<QueueOptions> { queueOptions } }));

        // Act
        var consumer = factory.CreateConsumer("Workday");

        // Assert
        Assert.NotNull(consumer);
        Assert.Equal("Workday", consumer.QueueName);
    }

    [Fact]
    public void CreateConsumerWithInvalidQueueNameShouldThrowArgumentException()
    {
        // Arrange
        var sqsClientMock = new Mock<IAmazonSQS>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(l => l.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

        var factory = new SqsEventConsumerFactory(sqsClientMock.Object, loggerFactoryMock.Object, Options.Create(new QueueConfiguration { Queues = new List<QueueOptions>() }));

        // Act & Assert
        Assert.Throws<ArgumentException>(() => factory.CreateConsumer("NonExistentQueue"));
    }

    [Fact]
    public void CreateConsumersShouldReturnAllConfiguredConsumers()
    {
        // Arrange
        var sqsClientMock = new Mock<IAmazonSQS>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(l => l.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

        var queues = new List<QueueOptions>
        {
            new QueueOptions { Name = "Workday", Url = "https://sqs.test/Workday" },
            new QueueOptions { Name = "SicsEvents", Url = "https://sqs.test/SicsEvents" }
        };

        var factory = new SqsEventConsumerFactory(sqsClientMock.Object, loggerFactoryMock.Object, Options.Create(new QueueConfiguration { Queues = queues }));

        // Act
        var consumers = factory.CreateConsumers().ToList();

        // Assert
        Assert.Equal(2, consumers.Count);
        Assert.Contains(consumers, c => c.QueueName == "Workday");
        Assert.Contains(consumers, c => c.QueueName == "SicsEvents");
    }
}
