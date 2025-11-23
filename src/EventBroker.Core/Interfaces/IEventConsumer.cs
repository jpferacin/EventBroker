using System;
using System.Threading;
using System.Threading.Tasks;

namespace SiriusPt.EventBroker.Core.Interfaces;

public interface IEventConsumer
{
    string QueueName { get; }

    /// <summary>
    /// Starts listening and invokes messageHandler when a message arrives.
    /// Maybe not correct to put here as it mix concerns of consumer and worker (polling)?
    /// </summary>
    Task StartAsync(Func<string, Task<bool>> messageHandler, int maxNumberOfMessages = 10, CancellationToken cancellationToken = default);
    Task StartAsync<T>(Func<T, Task<bool>> messageHandler, int maxNumberOfMessages = 10, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Fetch a single raw message from the queue for fine-grained control
    /// </summary>
    Task<string?> FetchMessageAsync(CancellationToken cancellationToken = default);
    Task<T?> FetchMessageAsync<T>(CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Copy a message to another queue.
    /// </summary>
    Task CopyMessageToQueueAsync(string messageBody, string targetQueueUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Move a message to the Dead Letter Queue with reason and metadata.
    /// </summary>
    Task MoveMessageToDeadLetterQueueAsync(string messageBody, string reason, CancellationToken cancellationToken = default);
}
