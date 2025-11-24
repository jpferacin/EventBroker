using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using SiriusPt.EventBroker.Transport.InMemory;
using Xunit;

namespace EventBroker.Transport.InMemory.Tests;

public class InMemoryTests
{
    [Fact]
    public async Task PublishAndConsumeShouldWork()
    {
        var queue = new ConcurrentQueue<string>();
        var publisher = new InMemoryEventPublisher(queue, "TestQueue");
        var consumer = new InMemoryEventConsumer(queue, "TestQueue");

        await publisher.PublishAsync(new { Id = 123, Name = "UnitTest" });

        var received = null as string;

        _ = consumer.StartAsync(async msg =>
        {
            received = msg;
            return await Task.FromResult(true);
        });

        await Task.Delay(500);

        Assert.NotNull(received);
    }
}
