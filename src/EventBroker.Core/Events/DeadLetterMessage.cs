using System;

namespace SiriusPt.EventBroker.Core.Events;

public class DeadLetterMessage
{
    public string OriginalMessage { get; set; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
    public string OriginalQueue { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime Timestamp { get; set; }
}

