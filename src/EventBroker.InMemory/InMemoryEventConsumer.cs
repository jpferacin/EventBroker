using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SiriusPt.EventBroker.Core.Interfaces;

namespace SiriusPt.EventBroker.Transport.InMemory;

public class InMemoryEventConsumer : IEventConsumer
{
    private readonly ConcurrentQueue<string> _queue;

    public string QueueName { get; }

    public InMemoryEventConsumer(string name)
    {
        _queue = new ConcurrentQueue<string>();
        QueueName = name;
    }

    public InMemoryEventConsumer(ConcurrentQueue<string> queue, string name)
    {
        _queue = queue;
        QueueName = name;
    }

    public async Task StartAsync(Func<string, Task<bool>> messageHandler, int maxNumberOfMessages = 10, CancellationToken cancellationToken = default)
    {
        await PollMessages(async body => await messageHandler(body), cancellationToken);
    }

    public async Task StartAsync<T>(Func<T, Task<bool>> messageHandler, int maxNumberOfMessages = 10, CancellationToken cancellationToken = default) where T : class
    {
        await PollMessages(async body =>
        {
            try
            {
                var obj = JsonSerializer.Deserialize<T>(body);
                if (obj is not null)
                {
                    return await messageHandler(obj);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Deserialization error: {ex.Message}");
            }
            return false;
        }, cancellationToken);
    }

    public async Task<string?> FetchMessageAsync(CancellationToken cancellationToken = default)
    {
        if (_queue.TryDequeue(out var message))
        {
            return message;
        }

        await Task.Delay(100, cancellationToken); // simulate wait
        return null;
    }

    public Task<T?> FetchMessageAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (_queue.TryDequeue(out var message))
        {
            try
            {
                var obj = JsonSerializer.Deserialize<T>(message);
                return Task.FromResult(obj);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Deserialization error: {ex.Message}");
                return Task.FromResult<T?>(null);
            }
        }

        return Task.FromResult<T?>(null);
    }

    private async Task PollMessages(Func<string, Task<bool>> handler, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_queue.TryDequeue(out var message))
            {
                try
                {
                    await handler(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing message: {ex.Message}");
                }
            }
            else
            {
                await Task.Delay(100, cancellationToken);
            }
        }
    }

    public Task CopyMessageToQueueAsync(string messageBody, string targetQueueUrl, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task MoveMessageToDeadLetterQueueAsync(string messageBody, string reason, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
