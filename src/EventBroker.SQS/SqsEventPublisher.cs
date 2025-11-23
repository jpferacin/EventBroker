using Amazon.SQS;
using Amazon.SQS.Model;
using SiriusPt.EventBroker.Core.Interfaces;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SiriusPt.EventBroker.Transport.SQS;

public class SqsEventPublisher : IEventPublisher
{
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;

    public string QueueName => new Uri(_queueUrl).Segments.Last().TrimEnd('/');

    public SqsEventPublisher(IAmazonSQS sqsClient, string queueUrl)
    {
        _sqsClient = sqsClient;
        _queueUrl = queueUrl;
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
    {
        var body = JsonSerializer.Serialize(message);
        var request = new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = body
        };
        await _sqsClient.SendMessageAsync(request, cancellationToken);
    }

    public async Task PublishAsync<T>(T message, string messageGroupId, CancellationToken cancellationToken = default)
    {
        var body = JsonSerializer.Serialize(message);
        var request = new SendMessageRequest
        {
            QueueUrl = _queueUrl, // must be FIFO queue (ends with .fifo)
            MessageBody = body,
            MessageGroupId = messageGroupId, // required for FIFO
            MessageDeduplicationId = Guid.NewGuid().ToString() // or hash of body
        };
        await _sqsClient.SendMessageAsync(request, cancellationToken);
    }
}
