using System.Threading;
using System.Threading.Tasks;

namespace SiriusPt.EventBroker.Core.Interfaces;

public interface IEventHandler<T>
{
    /// <summary>
    /// This method is where the business logic for processing the message will start.
    ///
    /// If the message was successfully processed and should them <see cref="EventProcessResult.Success"/> should be returned. The
    /// underlying service like SQS will be told the message has been processed and can be removed.
    ///
    /// If an exception is thrown from this method the status is treated as <see cref="EventProcessResult.Failed()"/>.
    /// </summary>
    /// <param name="messageEnvelope">The message read from the message source wrapped around a message envelope containing message metadata.</param>
    /// <param name="token">The optional cancellation token.</param>
    /// <returns>The status of the processed message. For example whether the message was successfully processed.</returns>
    Task<EventProcessResult> HandleAsync(T messageEnvelope, CancellationToken token = default);
}
