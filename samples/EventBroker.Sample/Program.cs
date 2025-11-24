using Amazon;
using Amazon.Runtime.CredentialManagement;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SiriusPt.EventBroker.Core.Events;
using SiriusPt.EventBroker.Transport.SQS;
using Amazon.SQS;
using SiriusPt.EventBroker.Transport.InMemory;

class Program
{

    public static CloudEvent<T> CreateCloudEvent<T>(T data, string source = "EventBroker.Sample")
    {
        return new CloudEvent<T>
        {
            Source = source,
            Data = data
        };
    }

    static async Task Main(string[] args)
    {
        var cts = new CancellationTokenSource();

        // Create LoggerFactory
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });

        var logger = loggerFactory.CreateLogger<SqsEventConsumer>();

        // AWS SQS Example with optional DLQ
        var sqsClient = new AmazonSQSClient();

        // Load credentials from AWS profile
        var chain = new CredentialProfileStoreChain();
        if (chain.TryGetAWSCredentials("DevOps-Sandbox", out var awsCredentials))
        {
            sqsClient = new AmazonSQSClient(awsCredentials, RegionEndpoint.EUCentral1);
        }
        else
        {
            Console.WriteLine("Profile not found: DevOps-Sandbox");
        }

        const string sqsUrl = "https://sqs.eu-central-1.amazonaws.com/041717511598/Sics-Events";
        var publisher = new SqsEventPublisher(sqsClient, sqsUrl);
        var consumer = new SqsEventConsumer(logger, sqsClient, sqsUrl); // DLQ optional

        Console.WriteLine("Publishing message to SQS...");
        await publisher.PublishAsync(CreateCloudEvent( new TestMessage { Id = 1, Name = "Test" }));

        Console.WriteLine("Starting SQS consumer with typed handler...");
        _ = consumer.StartAsync<CloudEvent<TestMessage>>(async evt =>
        {
            var msg = evt.Data;
            Console.WriteLine($"[SQS] Received typed message: Id={msg.Id}, Name={msg.Name}");
            cts.Cancel(); // Stop after first message for demo
            return await Task.FromResult(true);
        });


        Thread.Sleep(2000); // Wait a bit to ensure the message is available

        // InMemory Example for Tests
        var queue = new ConcurrentQueue<string>();
        var testPublisher = new InMemoryEventPublisher(queue);
        var testConsumer = new InMemoryEventConsumer(queue, "TestQueue");

        Console.WriteLine("Publishing message to InMemory queue...");
        await testPublisher.PublishAsync(new { Id = 2, Name = "InMemory" });

        Console.WriteLine("Starting InMemory consumer with typed handler...");
        _ = testConsumer.StartAsync<dynamic>(async msg =>
        {
            if (msg is JsonElement element)
            {
                var id = element.GetProperty("Id").GetInt32();
                var name = element.GetProperty("Name").GetString();
                Console.WriteLine($"[InMemory] Received dynamic message: Id={id}, Name={name}");
            }
            cts.Cancel(); // Stop after first message for demo
            return await Task.FromResult(true);
        });

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}