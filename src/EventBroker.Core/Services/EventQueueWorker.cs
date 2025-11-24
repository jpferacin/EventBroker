
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SiriusPt.EventBroker.Core;
using SiriusPt.EventBroker.Core.Helpers;
using SiriusPt.EventBroker.Core.Interfaces;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public abstract partial class EventQueueWorker : BackgroundService
{
    protected readonly IEventConsumer _consumer;
    protected readonly IEventHandlerInvoker _eventInvoker;
    protected readonly ILogger<EventQueueWorker> _logger;
    protected readonly IWorkerPollingOptions _pollingOptions;
    private readonly EventConfiguration _eventConfiguration;

    protected EventQueueWorker(
        ILogger<EventQueueWorker> logger,
        IEventConsumer consumer,
        IEventHandlerInvoker eventHandlerInvoker,
        EventConfiguration eventConfiguration,
        IWorkerPollingOptions pollingOptions)
    {
        _logger = logger;
        _consumer = consumer;
        _eventInvoker = eventHandlerInvoker;
        _eventConfiguration = eventConfiguration;
        _pollingOptions = pollingOptions;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueName = _consumer.QueueName;

        while (!stoppingToken.IsCancellationRequested)
        {
            var message = await _consumer.FetchMessageAsync(stoppingToken);
            if (message is null)
            {
                await Task.Delay(TimeSpan.FromSeconds(_pollingOptions.PollingIntervalSeconds), stoppingToken);
                continue;
            }

            bool success = false;

            try
            {
                if (CloudEventHelper.TryGetEventType(message, out var eventType))
                {
                    var result = await DispatchRawEventAsync(eventType, message, stoppingToken);
                    success = result.Success;
                }
                else
                {
                    LogUnknownEventType(queueName, message);
                    await _consumer.MoveMessageToDeadLetterQueueAsync(message, "Unknown event type", stoppingToken);
                }
            }
            catch (Exception ex)
            {
                LogProcessingError(queueName, message, ex);
                await _consumer.MoveMessageToDeadLetterQueueAsync(message, $"Exception: {ex.Message}", stoppingToken);
            }

            if (!success)
            {
                LogProcessingWarning(queueName, message);
                await _consumer.MoveMessageToDeadLetterQueueAsync(message, "Handler failed", stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(_pollingOptions.PollingIntervalSeconds), stoppingToken);
        }
    }

    public async Task<EventProcessResult> DispatchRawEventAsync(string eventType, string message, CancellationToken token)
    {
        var queueName = _consumer.QueueName;
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

    public async Task<EventProcessResult> DispatchRawEventAsync(string eventType, object typedEvent, CancellationToken token)
    {
        var queueName = _consumer.QueueName;
        var mapping = _eventConfiguration.GetHandlerForMessage(eventType);
        if (mapping is null)
        {
            LogHandlerMappingNotFound(queueName, eventType);
            return EventProcessResult.Fail($"No handler mapping found for event type: {eventType}");
        }

        return await _eventInvoker.InvokeHandlerAsync(mapping, typedEvent, token);
    }

    [LoggerMessage(EventId = 301, Level = LogLevel.Error, Message = "[{QueueName}] Error processing message: {Message}")]
    partial void LogProcessingError(string QueueName, string Message, Exception ex);

    [LoggerMessage(EventId = 302, Level = LogLevel.Warning, Message = "[{QueueName}] Message processing failed. Moving to DLQ: {Message}")]
    partial void LogProcessingWarning(string QueueName, string Message);

    [LoggerMessage(EventId = 303, Level = LogLevel.Warning, Message = "[{QueueName}] No handler mapping found for event type: {EventType}")]
    partial void LogHandlerMappingNotFound(string QueueName, string EventType);

    [LoggerMessage(EventId = 304, Level = LogLevel.Warning, Message = "[{QueueName}] Failed to deserialize message for event type: {EventType}")]
    partial void LogDeserializationFailed(string QueueName, string EventType);

    [LoggerMessage(EventId = 305, Level = LogLevel.Information, Message = "[{QueueName}] Unknown event type in message: {Message}")]
    partial void LogUnknownEventType(string QueueName, string Message);
}
