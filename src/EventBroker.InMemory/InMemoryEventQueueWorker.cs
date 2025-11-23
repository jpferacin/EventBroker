
using SiriusPt.EventBroker.Core;
using SiriusPt.EventBroker.Core.Helpers;
using SiriusPt.EventBroker.Core.Interfaces;
using SiriusPt.EventBroker.Core.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SiriusPt.EventBroker.Transport.InMemory;

public partial class InMemoryEventQueueWorker : EventQueueWorker
{
    public InMemoryEventQueueWorker(
        ILogger<EventQueueWorker> logger,
        IEventConsumer consumer,
        IEventHandlerInvoker eventHandlerInvoker,
        EventConfiguration eventConfiguration,
        IWorkerPollingOptions pollingOptions)
        : base(logger, consumer, eventHandlerInvoker, eventConfiguration, pollingOptions)
    {
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
                    LogUnknownEventType(message);
                }
            }
            catch (Exception ex)
            {
                LogProcessingError(message, ex);
            }

            if (!success)
            {
                LogProcessingWarning(message);
            }

            await Task.Delay(TimeSpan.FromSeconds(_pollingOptions.PollingIntervalSeconds), stoppingToken);
        }
    }

    [LoggerMessage(EventId = 501, Level = LogLevel.Information, Message = "Unknown event type in InMemory message: {Message}")]
    partial void LogUnknownEventType(string Message);

    [LoggerMessage(EventId = 502, Level = LogLevel.Error, Message = "Error processing InMemory message: {Message}")]
    partial void LogProcessingError(string Message, Exception ex);

    [LoggerMessage(EventId = 503, Level = LogLevel.Warning, Message = "InMemory message processing failed: {Message}")]
    partial void LogProcessingWarning(string Message);
}
