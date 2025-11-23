
using Amazon.SQS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SiriusPt.EventBroker.Core;
using SiriusPt.EventBroker.Core.Interfaces;
using SiriusPt.EventBroker.Transport.SQS;
using System;
using System.Collections.Generic;
using System.Linq;

public class SqsEventConsumerFactory : IEventConsumerFactory
{
    private readonly IAmazonSQS _sqsClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly QueueConfiguration _queueConfiguration;

    public SqsEventConsumerFactory(IAmazonSQS sqsClient, ILoggerFactory loggerFactory, IOptions<QueueConfiguration> queueOptions)
    {
        _sqsClient = sqsClient;
        _loggerFactory = loggerFactory;
        _queueConfiguration = queueOptions.Value;
    }

    public IEventConsumer CreateConsumer(string queueName)
    {
        var logger = _loggerFactory.CreateLogger<SqsEventConsumer>();
        var options =
            _queueConfiguration.Queues.FirstOrDefault(q => q.Name.Equals(queueName, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($"No queue found with name {queueName}");

        return new SqsEventConsumer(logger, _sqsClient, options.Url, options.DLQUrl);
    }

    public IEventConsumer CreateConsumer(QueueOptions options)
    {
        var logger = _loggerFactory.CreateLogger<SqsEventConsumer>();
        return new SqsEventConsumer(logger, _sqsClient, options.Url, options.DLQUrl);
    }

    public IEnumerable<IEventConsumer> CreateConsumers()
    {
        foreach (var options in _queueConfiguration.Queues)
        {
            yield return CreateConsumer(options);
        }
    }
}
