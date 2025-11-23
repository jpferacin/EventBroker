using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SiriusPt.EventBroker.Core.Interfaces;

namespace SiriusPt.EventBroker.Transport.InMemory;

public class InMemoryEventPublisher : IEventPublisher
{
    private readonly ConcurrentQueue<string> _queue;

    public InMemoryEventPublisher(ConcurrentQueue<string> queue, string? queueName = "dummy")
    {
        _queue = queue;
    }

    public string QueueName => throw new System.NotImplementedException();

    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
    {
        var body = JsonSerializer.Serialize(message);
        _queue.Enqueue(body);
        return Task.CompletedTask;
    }

    public Task PublishAsync<T>(T message, string messageGroupId, CancellationToken cancellationToken = default)
    {
        var body = JsonSerializer.Serialize(message);
        _queue.Enqueue(body);
        return Task.CompletedTask;
    }
}
