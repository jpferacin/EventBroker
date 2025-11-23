using System.Collections.Generic;

namespace SiriusPt.EventBroker.Core.Interfaces;

public interface IEventConsumerFactory
{
    IEventConsumer CreateConsumer(QueueOptions options);
    IEventConsumer CreateConsumer(string queueName);
    IEnumerable<IEventConsumer> CreateConsumers();
}
