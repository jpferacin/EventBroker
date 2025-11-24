using Microsoft.AspNetCore.Mvc;
using SiriusPt.EventBroker.Core;
using SiriusPt.EventBroker.Core.Interfaces;

namespace EventBroker.Service.Sample.Endpoints;

public static class HealthEndpoints
{
    public static RouteGroupBuilder MapEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/");

        group.MapGet("/healthz", () => Results.Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow
        }));

        group.MapGet("/ping", () => Results.Ok("pong"));

        group.MapGet("/status", (IEnumerable<IEventConsumer> consumers) =>
        {
            return Results.Ok(new
            {
                Worker = "MultiQueueEventWorker",
                Queues = consumers.Select(c => c.QueueName),
                LastHeartbeat = MultiQueueMetrics.LastHeartbeat
            });
        });

        group.MapGet("/queues", (IEnumerable<IEventConsumer> consumers) =>
        {
            return Results.Ok(consumers.Select(c => new
            {
                QueueName = c.QueueName,
                Type = c.GetType().Name
            }));
        });


        group.MapGet("/metrics", () =>
        {
            return Results.Ok(new
            {
                LastHeartbeat = MultiQueueMetrics.LastHeartbeat,
                Queues = MultiQueueMetrics.QueueStats.Select(q => new
                {
                    QueueName = q.Key,
                    Processed = q.Value.Processed,
                    Failed = q.Value.Failed,
                    FailureReasons = q.Value.FailureReasons.Select(r => new { Reason = r.Key, Count = r.Value })
                })
            });
        });

        group.MapPost("/publish/{queueName}", async (
            string queueName,
            [FromBody] object messageBody,
            IEventPublisherFactory publisherFactory) =>
        {
            try
            {
                var publisher = publisherFactory.CreatePublisher(queueName);
                await publisher.PublishAsync(messageBody);

                return Results.Ok(new
                {
                    Status = "Message published successfully",
                    Queue = queueName
                });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to publish message to {queueName}. Error: {ex.Message}");
            }
        });

        return group;
    }
}
