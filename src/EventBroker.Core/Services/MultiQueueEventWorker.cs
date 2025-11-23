
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SiriusPt.EventBroker.Core.Helpers;
using SiriusPt.EventBroker.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SiriusPt.EventBroker.Core.Services;

public partial class MultiQueueEventWorker : BackgroundService
{
    private readonly IEnumerable<IEventConsumer> _consumers;
    private readonly IEventHandlerInvoker _eventInvoker;
    private readonly EventConfiguration _eventConfiguration;
    private readonly ILogger<MultiQueueEventWorker> _logger;
    private readonly TimeSpan _pollingIntervalSeconds;
    private readonly int _heartbeatIntervalSeconds;

    public MultiQueueEventWorker(
        ILogger<MultiQueueEventWorker> logger,
        IEventConsumerFactory factory,  //IEnumerable<IEventConsumer> consumers,
        IEventHandlerInvoker eventInvoker,
        EventConfiguration eventConfiguration,
        IOptions<WorkerPollingOptions> workerOptions)
    {
        _logger = logger;
        _consumers = factory.CreateConsumers(); //consumers;
        _eventInvoker = eventInvoker;
        _eventConfiguration = eventConfiguration;

        var options = workerOptions.Value;
        _heartbeatIntervalSeconds = options.HeartbeatIntervalSeconds;
        _pollingIntervalSeconds = TimeSpan.FromSeconds(options.PollingIntervalSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var lastHeartbeat = DateTime.UtcNow.AddHours(-8); // this will trigger a heartbeat at startup

        while (!stoppingToken.IsCancellationRequested)
        {
            var tasks = _consumers.Select(async consumer =>
            {
                var queueName = consumer.QueueName;
                var message = await consumer.FetchMessageAsync(stoppingToken);
                if (message is null)
                {
                    return;
                }

                try
                {
                    if (_eventConfiguration.LogMessageContent)
                    {
                        LogProcessingMessage(queueName, message);
                    }

                    if (!IsValidJson(message))
                    {
                        var errorDetails = "Invalid JSON format";
                        LogInvalidJson(queueName, message);
                        MultiQueueMetrics.IncrementFailed(queueName, errorDetails);
                        await consumer.MoveMessageToDeadLetterQueueAsync(message, errorDetails, stoppingToken);
                        return;
                    }

                    if (CloudEventHelper.TryGetEventType(message, out var eventType))
                    {
                        var result = await DispatchRawEventAsync(queueName, eventType, message, stoppingToken);
                        if (result.Success)
                        {
                            MultiQueueMetrics.IncrementProcessed(queueName);
                        }
                        else
                        {
                            var errorDetails = $"Handler failed: {result.Error}";
                            LogProcessingWarning(queueName, message);
                            MultiQueueMetrics.IncrementFailed(queueName, errorDetails);
                            await consumer.MoveMessageToDeadLetterQueueAsync(message, errorDetails, stoppingToken);
                        }
                    }
                    else
                    {
                        var errorDetails = "Unknown event type";
                        LogUnknownEventType(queueName, message);
                        MultiQueueMetrics.IncrementFailed(queueName, errorDetails);
                        await consumer.MoveMessageToDeadLetterQueueAsync(message, errorDetails, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    var errorDetails = $"Exception: {ex.Message}";
                    LogProcessingError(queueName, message, ex);
                    MultiQueueMetrics.IncrementFailed(queueName, errorDetails);
                    await consumer.MoveMessageToDeadLetterQueueAsync(message, errorDetails, stoppingToken);
                }
            });

            await Task.WhenAll(tasks);

            // Heartbeat
            if ((DateTime.UtcNow - lastHeartbeat).TotalSeconds >= _heartbeatIntervalSeconds)
            {
                LogHeartbeat($"Worker is alive. Monitoring {_consumers.Count()} queues.");
                MultiQueueMetrics.UpdateHeartbeat();
                lastHeartbeat = DateTime.UtcNow;
            }

            await Task.Delay(_pollingIntervalSeconds, stoppingToken);
        }
    }

    private async Task<EventProcessResult> DispatchRawEventAsync(string queueName, string eventType, string message, CancellationToken token)
    {
        var mapping = _eventConfiguration.GetHandlerForMessage(eventType);
        if (mapping is null)
        {
            LogHandlerMappingNotFound(queueName, eventType);
            return EventProcessResult.Fail($"No handler mapping found for event type: {eventType}");
        }

        var normalizedJson = CloudEventHelper.UnwrapPayload(message);
        var typedEvent = JsonSerializer.Deserialize(normalizedJson, mapping.MessageType, _eventConfiguration.SerializationOptions);
        if (typedEvent is null)
        {
            LogDeserializationFailed(queueName, eventType);
            return EventProcessResult.Fail($"Failed to deserialize message for event type: {eventType}");
        }

        return await _eventInvoker.InvokeHandlerAsync(mapping, typedEvent, token);
    }

    private static bool IsValidJson(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        try
        {
            JsonDocument.Parse(input);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    [LoggerMessage(EventId = 600, Level = LogLevel.Information, Message = "[{QueueName}] message received: {Message}")]
    partial void LogProcessingMessage(string QueueName, string Message);

    [LoggerMessage(EventId = 601, Level = LogLevel.Error, Message = "[{QueueName}] Error processing message: {Message}")]
    partial void LogProcessingError(string QueueName, string Message, Exception ex);

    [LoggerMessage(EventId = 602, Level = LogLevel.Warning, Message = "[{QueueName}] Message processing failed: {Message}")]
    partial void LogProcessingWarning(string QueueName, string Message);

    [LoggerMessage(EventId = 603, Level = LogLevel.Warning, Message = "[{QueueName}] No handler mapping found for event type: {EventType}")]
    partial void LogHandlerMappingNotFound(string QueueName, string EventType);

    [LoggerMessage(EventId = 604, Level = LogLevel.Warning, Message = "[{QueueName}] Failed to deserialize message for event type: {EventType}")]
    partial void LogDeserializationFailed(string QueueName, string EventType);

    [LoggerMessage(EventId = 605, Level = LogLevel.Information, Message = "[{QueueName}] Unknown event type in message: {Message}")]
    partial void LogUnknownEventType(string QueueName, string Message);

    [LoggerMessage(EventId = 606, Level = LogLevel.Warning, Message = "[{QueueName}] Invalid JSON message: {Message}")]
    partial void LogInvalidJson(string QueueName, string Message);

    [LoggerMessage(EventId = 607, Level = LogLevel.Information, Message = "Heartbeat: {Message}")]
    partial void LogHeartbeat(string Message);

}
