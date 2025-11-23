using System.Collections.Generic;

namespace SiriusPt.EventBroker.Core;

public class QueueConfiguration
{
    public List<QueueOptions> Queues { get; set; } = new();
}
