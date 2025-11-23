
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SiriusPt.EventBroker.Core;
using SiriusPt.EventBroker.Core.Interfaces;
using SiriusPt.EventBroker.Transport.InMemory;
using System.Collections.Generic;

public class InMemoryEventConsumerFactory : IEventConsumerFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly QueueConfiguration _queueConfiguration;

    public InMemoryEventConsumerFactory(ILoggerFactory loggerFactory, IOptions<QueueConfiguration> queueOptions)
    {
        _loggerFactory = loggerFactory;
        _queueConfiguration = queueOptions.Value;
    }

    public IEventConsumer CreateConsumer(QueueOptions options)
    {
        return new InMemoryEventConsumer(new(), options.Name);
    }

    public IEventConsumer CreateConsumer(string queueName)
    {
        return new InMemoryEventConsumer(new(), queueName);
    }

    public IEnumerable<IEventConsumer> CreateConsumers()
    {
        foreach (var options in _queueConfiguration.Queues)
        {
            yield return CreateConsumer(options);
        }
    }
}
