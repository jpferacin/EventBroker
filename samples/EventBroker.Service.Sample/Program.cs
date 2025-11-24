using Amazon;
using EventBroker.Extensions;
using EventBroker.Service.Sample.Endpoints;
using Microsoft.AspNetCore.Mvc;
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

app.MapEndpoints();

AppDomain.CurrentDomain.UnhandledException += (_, e) => app.StopAsync();

app.Run();