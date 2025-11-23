using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace SiriusPt.EventBroker.Core;


public static class MultiQueueMetrics
{
    private static readonly ConcurrentDictionary<string, QueueStats> _queueStats = new();

    public static IReadOnlyDictionary<string, QueueStats> QueueStats => _queueStats;
    public static DateTime LastHeartbeat { get; private set; } = DateTime.UtcNow;

    public static void IncrementProcessed(string queueName)
    {
        var stats = _queueStats.GetOrAdd(queueName, _ => new QueueStats());
        Interlocked.Increment(ref stats.Processed);
    }

    public static void IncrementFailed(string queueName, string reason)
    {
        var stats = _queueStats.GetOrAdd(queueName, _ => new QueueStats());
        Interlocked.Increment(ref stats.Failed);
        stats.FailureReasons.AddOrUpdate(reason, 1, (_, count) => count + 1);
    }

    public static void UpdateHeartbeat() => LastHeartbeat = DateTime.UtcNow;
}


public class QueueStats
{
    public long Processed;
    public long Failed;
    public ConcurrentDictionary<string, long> FailureReasons { get; } = new();
}


