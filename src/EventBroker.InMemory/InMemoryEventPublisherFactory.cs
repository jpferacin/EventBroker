
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SiriusPt.EventBroker.Core;
using SiriusPt.EventBroker.Core.Interfaces;
using SiriusPt.EventBroker.Transport.InMemory;
using System;
using System.Collections.Generic;
using System.Linq;

public class InMemoryEventPublisherFactory : IEventPublisherFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly QueueConfiguration _queueConfiguration;

    public InMemoryEventPublisherFactory(ILoggerFactory loggerFactory, IOptions<QueueConfiguration> queueOptions)
    {
        _loggerFactory = loggerFactory;
        _queueConfiguration = queueOptions.Value;
    }

    public IEventPublisher CreatePublisher(string queueName)
    {
        var uri = new Uri(queueName);
        if (uri.IsAbsoluteUri)
        {
            return new InMemoryEventPublisher(new(), queueName);
        }

        var options = _queueConfiguration.Queues.FirstOrDefault(q => q.Name.Equals(queueName, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"No queue found with name {queueName}");

        return new InMemoryEventPublisher(new(), options.Name);
    }

    public IEventPublisher CreatePublisher(QueueOptions options)
    {
        return new InMemoryEventPublisher(new(), options.Name);
    }

    public IEnumerable<IEventPublisher> CreatePublishers()
    {
        foreach (var options in _queueConfiguration.Queues)
        {
            yield return CreatePublisher(options.Name);
        }
    }
}