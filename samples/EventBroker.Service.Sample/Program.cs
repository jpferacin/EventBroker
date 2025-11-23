using Amazon;
using EventBroker.Extensions;
using SiriusPt.EventBroker.Core;
using SiriusPt.EventBroker.Core.Extensions;
using SiriusPt.EventBroker.Core.Interfaces;
using SiriusPt.EventBroker.Core.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
    {
        Indented = true
    };
});

// Bind Options from appsettings.json
builder.Services.Configure<WorkerPollingOptions>(builder.Configuration.GetSection("Worker"));
builder.Services.Configure<QueueConfiguration>(builder.Configuration.GetSection("Queues"));

// Register AWS SQS with profile
builder.Services.AddAwsSqsWithProfile("DevOps-Sandbox", RegionEndpoint.EUCentral1);

// Register AWS SQS factory
builder.Services.AddSingleton<IEventConsumerFactory, SqsEventConsumerFactory>();

// Register Event Broker Core services
builder.Services.AddEventCore(cfg =>
{
    cfg.RegisterSubscribers()
       .WithMessageSource("Workday.Publisher")
       .WithMessageContentLogging(true);
});

// Register MultiQueueEventWorker as Hosted Service
builder.Services.AddHostedService<MultiQueueEventWorker>();

// Register Publisher and Consumer using QueueOptions
builder.Services.AddSingleton<IEventPublisherFactory, SqsEventPublisherFactory>();
//builder.Services.AddSingleton<IEventPublisher>(sp =>
//{
//    var sqsClient = sp.GetRequiredService<IAmazonSQS>();
//    var options = sp.GetRequiredService<IOptions<QueueOptions>>().Value;
//    return new SqsEventPublisher(sqsClient, options.Url);
//});


// Add OpenAPI/Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable Swagger UI and OpenAPI JSON
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.MapGet("/healthz", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow
}));

app.MapGet("/ping", () => Results.Ok("pong"));

app.MapGet("/status", (IEnumerable<IEventConsumer> consumers) =>
{
    return Results.Ok(new
    {
        Worker = "MultiQueueEventWorker",
        Queues = consumers.Select(c => c.QueueName),
        LastHeartbeat = MultiQueueMetrics.LastHeartbeat
    });
});

app.MapGet("/queues", (IEnumerable<IEventConsumer> consumers) =>
{
    return Results.Ok(consumers.Select(c => new
    {
        QueueName = c.QueueName,
        Type = c.GetType().Name
    }));
});


app.MapGet("/metrics", () =>
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


app.MapPost("/publish/{queueName}", async (
    string queueName,
    [Microsoft.AspNetCore.Mvc.FromBody] string messageBody,
    HttpContext context,
    IEventPublisherFactory publisherFactory) =>
{
    //using var reader = new StreamReader(context.Request.Body);
    //var messageBody = await reader.ReadToEndAsync();

    if (string.IsNullOrWhiteSpace(messageBody))
    {
        return Results.BadRequest(new { Error = "Message body cannot be empty." });
    }

    try
    {
        var publisher = publisherFactory.CreatePublisher(queueName);
        await publisher.PublishAsync(messageBody);

        return Results.Ok(new
        {
            Status = "Message published successfully",
            Queue = queueName,
            Length = messageBody.Length
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to publish message to {queueName}. Error: {ex.Message}");
    }
});



AppDomain.CurrentDomain.UnhandledException += (_, e) => app.StopAsync();

app.Run();