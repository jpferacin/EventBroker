using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using SiriusPt.EventBroker.InMemory;
using Xunit;

namespace SiriusPt.EventBroker.Tests;

public class InMemoryTests
{
    [Fact]
    public async Task PublishAndConsume_ShouldWork()
    {
        var queue = new ConcurrentQueue<string>();
        var publisher = new InMemoryEventPublisher(queue);
        var consumer = new InMemoryEventConsumer(queue);

        await publisher.PublishAsync(new { Id = 123, Name = "UnitTest" });

        var cts = new CancellationTokenSource();
        string received = null;

        _ = consumer.StartAsync(async msg =>
        {
            received = msg;
            cts.Cancel();
        }, cts.Token);

        await Task.Delay(500);

        Assert.NotNull(received);
    }
}
