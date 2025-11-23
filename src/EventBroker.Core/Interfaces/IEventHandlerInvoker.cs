using System.Threading;
using System.Threading.Tasks;

namespace SiriusPt.EventBroker.Core.Interfaces
{
    public interface IEventHandlerInvoker
    {
        Task<EventProcessResult> InvokeHandlerAsync(SubscriberMapping mapping, object typedEvent, CancellationToken token = default);
    }
}