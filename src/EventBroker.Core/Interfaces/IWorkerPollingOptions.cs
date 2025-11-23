using System;

namespace SiriusPt.EventBroker.Core.Interfaces;

public interface IWorkerPollingOptions
{
    public int HeartbeatIntervalSeconds { get; init; }
    public int PollingIntervalSeconds { get; init; }
}
