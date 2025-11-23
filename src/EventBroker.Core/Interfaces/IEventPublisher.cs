using System.Threading;
using System.Threading.Tasks;

namespace SiriusPt.EventBroker.Core.Interfaces;

public interface IEventPublisher
{
    string QueueName { get; }

    //PublishAsync<T>: Publishes a message (serialized as JSON).
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default);
    Task PublishAsync<T>(T message, string messageGroupId, CancellationToken cancellationToken = default);
}
