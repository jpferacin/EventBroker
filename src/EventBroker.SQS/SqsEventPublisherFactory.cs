
using Amazon.SQS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SiriusPt.EventBroker.Core;
using SiriusPt.EventBroker.Core.Interfaces;
using SiriusPt.EventBroker.Transport.SQS;
using System;
using System.Collections.Generic;
using System.Linq;

public class SqsEventPublisherFactory : IEventPublisherFactory
{
    private readonly IAmazonSQS _sqsClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly QueueConfiguration _queueConfiguration;

    public SqsEventPublisherFactory(IAmazonSQS sqsClient, ILoggerFactory loggerFactory, IOptions<QueueConfiguration> queueOptions)
    {
        _sqsClient = sqsClient;
        _loggerFactory = loggerFactory;
        _queueConfiguration = queueOptions.Value;
    }

    public IEventPublisher CreatePublisher(string queueName)
    {
        //var logger = _loggerFactory.CreateLogger<SqsEventPublisher>();

        //check if queueName is a valid Uri or a simple name
        var uri = new Uri(queueName);
        if (uri.IsAbsoluteUri)
        {
            return new SqsEventPublisher(_sqsClient, queueName);
        }

        var options =
            _queueConfiguration.Queues.FirstOrDefault(q => q.Name.Equals(queueName, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($"No queue found with name {queueName}");

        return new SqsEventPublisher(_sqsClient, options.Url);
    }

    public IEventPublisher CreatePublisher(QueueOptions options)
    {
        //var logger = _loggerFactory.CreateLogger<SqsEventPublisher>();
        return new SqsEventPublisher(_sqsClient, options.Url);
    }

    public IEnumerable<IEventPublisher> CreatePublishers()
    {
        foreach (var options in _queueConfiguration.Queues)
        {
            yield return CreatePublisher(options);
        }
    }
}