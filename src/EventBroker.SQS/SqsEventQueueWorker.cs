
using Amazon.S3.Util;
using SiriusPt.EventBroker.Core.Helpers;
using SiriusPt.EventBroker.Core;
using SiriusPt.EventBroker.Core.Interfaces;
using SiriusPt.EventBroker.Core.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SiriusPt.EventBroker.Transport.SQS;

public partial class SqsEventQueueWorker : EventQueueWorker
{
    public SqsEventQueueWorker(
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
                if (!CloudEventHelper.IsValidJson(message))
                {
                    LogInvalidJsonEventType(queueName, message);
                    continue;
                }

                if (CloudEventHelper.TryGetEventType(message, out var eventType))
                {
                    var result = await DispatchRawEventAsync(eventType, message, stoppingToken);
                    success = result.Success;
                }
                else if (TryGetS3EventNotification(message, out var s3EventNotification))
                {
                    var result = await DispatchRawEventAsync(nameof(S3EventNotification), s3EventNotification, stoppingToken);
                    success = result.Success;
                }
                else
                {
                    LogUnknownEventType(queueName, message);
                }
            }
            catch (Exception ex)
            {
                LogProcessingError(queueName, message, ex);
            }

            if (!success)
            {
                LogProcessingWarning(queueName, message);
            }

            await Task.Delay(TimeSpan.FromSeconds(_pollingOptions.PollingIntervalSeconds), stoppingToken);
        }
    }

    private static bool TryGetS3EventNotification(string json, out S3EventNotification eventNotification)
    {
        eventNotification = null!;
        try
        {
            var s3Event = S3EventNotification.ParseJson(json);
            if (s3Event?.Records is null || s3Event.Records.Count == 0)
            {
                return false;
            }

            eventNotification = s3Event;
            return true;
        }
        catch
        {
            return false;
        }
    }

    [LoggerMessage(EventId = 400, Level = LogLevel.Warning, Message = "[{QueueName}] Not JSON event type in SQS message: {Message}")]
    partial void LogInvalidJsonEventType(string QueueName, string Message);

    [LoggerMessage(EventId = 401, Level = LogLevel.Warning, Message = "[{QueueName}] Unknown event type in SQS message: {Message}")]
    partial void LogUnknownEventType(string QueueName, string Message);

    [LoggerMessage(EventId = 402, Level = LogLevel.Error, Message = "[{QueueName}] Error processing SQS message: {Message}")]
    partial void LogProcessingError(string QueueName, string Message, Exception ex);

    [LoggerMessage(EventId = 403, Level = LogLevel.Warning, Message = "[{QueueName}] SQS message processing failed. Moving to DLQ: {Message}")]
    partial void LogProcessingWarning(string QueueName, string Message);
}
