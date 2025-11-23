using System.Collections.Generic;

namespace SiriusPt.EventBroker.Core.Interfaces;

public interface IEventPublisherFactory
{
    IEventPublisher CreatePublisher(QueueOptions options);
    IEventPublisher CreatePublisher(string queueName);
    IEnumerable<IEventPublisher> CreatePublishers();
}
