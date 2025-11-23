using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using SiriusPt.EventBroker.Core.Events;
using SiriusPt.EventBroker.Core.Helpers;
using SiriusPt.EventBroker.Core.Interfaces;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SiriusPt.EventBroker.Transport.SQS;

public partial class SqsEventConsumer : IEventConsumer
{
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;
    private readonly string? _deadLetterQueueUrl;
    private readonly int _maxRetries = 3;
    private readonly ILogger<SqsEventConsumer> _logger;

    public string QueueName => new Uri(_queueUrl).Segments.Last().TrimEnd('/');

    public SqsEventConsumer(ILogger<SqsEventConsumer> logger, IAmazonSQS sqsClient, string queueUrl, string? deadLetterQueueUrl = null)
    {
        _sqsClient = sqsClient;
        _queueUrl = queueUrl;
        _deadLetterQueueUrl = deadLetterQueueUrl;
        _logger = logger;
    }

    public async Task StartAsync(Func<string, Task<bool>> messageHandler, int maxNumberOfMessages = 10, CancellationToken cancellationToken = default)
        => await PollMessages(async body => await messageHandler(body), 10, 20, cancellationToken);

    public async Task StartAsync<T>(Func<T, Task<bool>> messageHandler, int maxNumberOfMessages = 10, CancellationToken cancellationToken = default) where T : class
        => await PollMessages(async body =>
        {
            try
            {
                var normalizedJson = CloudEventHelper.UnwrapPayload(body);
                var obj = JsonSerializer.Deserialize<T>(normalizedJson);
                if (obj is not null)
                {
                    return await messageHandler(obj);
                }
            }
            catch (Exception ex)
            {
                LogDeserializationError(ex.Message);
            }
            return false;
        }, 10, 20, cancellationToken);

    public async Task<string?> FetchMessageAsync(CancellationToken cancellationToken = default)
    {
        var response = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = _queueUrl,
            MaxNumberOfMessages = 1,
            WaitTimeSeconds = 5 // short polling or long polling
        }, cancellationToken);

        if (response.Messages is null || response.Messages.Count == 0)
        {
            return null;
        }   

        var msg = response.Messages.First();

        // Delete after fetching (or leave for manual ack if needed)
        await _sqsClient.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, cancellationToken);
        
        return CloudEventHelper.UnwrapPayload(msg.Body);
    }

    public async Task<T?> FetchMessageAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        var response = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = _queueUrl,
            MaxNumberOfMessages = 1,
            WaitTimeSeconds = 10 // long polling
        }, cancellationToken);

        if (response.Messages is null || response.Messages.Count == 0)
        {
            return null; 
        }

        var msg = response.Messages.First();

        try
        {
            var normalizedJson = CloudEventHelper.UnwrapPayload(msg.Body);
            var obj = JsonSerializer.Deserialize<T>(normalizedJson);

            // Delete message after successful deserialization
            await _sqsClient.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, cancellationToken);

            return obj;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch and process message: {MessageId}", msg.MessageId);
            // Optionally move to DLQ
            if (!string.IsNullOrEmpty(_deadLetterQueueUrl))
            {
                await MoveMessageToDeadLetterQueueAsync(msg.Body, ex.Message, cancellationToken);
                await _sqsClient.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, cancellationToken);
            }

            return null;
        }
    }

    private async Task PollMessages(Func<string, Task<bool>> handler, int maxNumberOfMessages = 10, int waitTimeSeconds = 20, CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var response = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = _queueUrl,
                MaxNumberOfMessages = maxNumberOfMessages,
                WaitTimeSeconds = waitTimeSeconds
            }, cancellationToken);

            if (response.Messages is null || response.Messages.Count == 0)
            {
                continue;
            }

            foreach (var msg in response.Messages)
            {
                int attempt = 0;
                bool success = false;

                while (attempt < _maxRetries && !success)
                {
                    try
                    {
                        success = await handler(msg.Body);
                        if (!success)
                        {
                            throw new Exception($"Message handler returned false. (Attempt {attempt + 1})");
                        }
                        await _sqsClient.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        attempt++;
                        LogProcessingError(ex.Message, attempt);
                    }
                }

                if (!success)
                {
                    if (!string.IsNullOrEmpty(_deadLetterQueueUrl))
                    {
                        LogMoveToDlq(msg.MessageId);
                        await MoveMessageToDeadLetterQueueAsync(msg.Body, "Handler failed", cancellationToken);
                    }
                    else
                    {
                        LogNoDlqConfigured(msg.MessageId);
                    }
                    await _sqsClient.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, cancellationToken);
                }
            }
        }
    }

    public async Task CopyMessageToQueueAsync(string messageBody, string targetQueueUrl, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(targetQueueUrl))
        {
            var request = new SendMessageRequest
            {
                QueueUrl = targetQueueUrl,
                MessageBody = messageBody
            };

            await _sqsClient.SendMessageAsync(request, cancellationToken);
        }
    }

    public async Task MoveMessageToDeadLetterQueueAsync(string messageBody, string reason, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(_deadLetterQueueUrl))
        {
            var dlqPayload = new DeadLetterMessage
            {
                OriginalMessage = messageBody,
                FailureReason = reason,
                OriginalQueue = _queueUrl,
                Timestamp = DateTime.UtcNow
            };

            var request = new SendMessageRequest
            {
                QueueUrl = _deadLetterQueueUrl!,
                MessageBody = JsonSerializer.Serialize(dlqPayload)
            };

            await _sqsClient.SendMessageAsync(request, cancellationToken);
        }
    }

    [LoggerMessage(EventId = 100, Level = LogLevel.Error, Message = "Error processing message: {Error}. Attempt {Attempt}")]
    partial void LogProcessingError(string Error, int Attempt);

    [LoggerMessage(EventId = 101, Level = LogLevel.Error, Message = "Deserialization error: {Error}")]
    partial void LogDeserializationError(string Error);

    [LoggerMessage(EventId = 102, Level = LogLevel.Warning, Message = "Moving message {MessageId} to Dead-Letter Queue.")]
    partial void LogMoveToDlq(string MessageId);

    [LoggerMessage(EventId = 103, Level = LogLevel.Warning, Message = "No DLQ configured. Message {MessageId} will be logged only.")]
    partial void LogNoDlqConfigured(string MessageId);
}