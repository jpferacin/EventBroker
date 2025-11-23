using Amazon.SQS;
using EventBroker.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SiriusPt.EventBroker.AWS;
using SiriusPt.EventBroker.InMemory;
using System.Collections.Concurrent;

namespace EventBroker.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers AWS SQS Event Broker services with optional Dead-Letter Queue.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="queueUrl">Main SQS queue URL.</param>
        /// <param name="deadLetterQueueUrl">Optional DLQ URL.</param>
        public static IServiceCollection AddAwsEventBroker(this IServiceCollection services, string queueUrl, string? deadLetterQueueUrl = null)
        {
            services.AddAWSService<IAmazonSQS>();
            services.AddSingleton<IEventPublisher>(sp => new SqsEventPublisher(sp.GetRequiredService<IAmazonSQS>(), queueUrl));
            services.AddSingleton<IEventConsumer>(sp =>
            {
                var sqsClient = sp.GetRequiredService<IAmazonSQS>();
                var logger = sp.GetRequiredService<ILogger<SqsEventConsumer>>();
                return new SqsEventConsumer(sqsClient, queueUrl, logger, deadLetterQueueUrl);
            });
            return services;
        }


        public static IServiceCollection AddInMemoryEventBroker(this IServiceCollection services)
        {
            var queue = new ConcurrentQueue<string>();
            services.AddSingleton<IEventPublisher>(sp => new InMemoryEventPublisher(queue));
            services.AddSingleton<IEventConsumer>(sp => new InMemoryEventConsumer(queue));
            return services;
        }
    }
}
