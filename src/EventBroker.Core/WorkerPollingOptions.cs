using SiriusPt.EventBroker.Core.Interfaces;

namespace SiriusPt.EventBroker.Core;

public class WorkerPollingOptions : IWorkerPollingOptions
{
    public int HeartbeatIntervalSeconds { get; init; }
    public int PollingIntervalSeconds { get; init; }
}
