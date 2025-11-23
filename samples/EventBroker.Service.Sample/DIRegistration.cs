using Amazon;
using Amazon.Runtime.CredentialManagement;
using Amazon.SQS;
using SiriusPt.EventBroker.Core.Interfaces;
using SiriusPt.EventBroker.Transport.SQS;

namespace EventBroker.Extensions
{
    public static class ServiceCollectionExtensions
    {

        public static IServiceCollection AddAwsSqsWithProfile(
            this IServiceCollection services,
            string profileName,
            RegionEndpoint region)
        {
            // Load credentials from AWS profile
            var chain = new CredentialProfileStoreChain();
            if (chain.TryGetAWSCredentials(profileName, out var awsCredentials))
            {
                // Register AmazonSQSClient with profile credentials
                services.AddSingleton<IAmazonSQS>(sp =>
                    new AmazonSQSClient(awsCredentials, region));
            }
            else
            {
                throw new InvalidOperationException($"AWS profile '{profileName}' not found.");
            }

            return services;
        }

        /// <summary>
        /// Registers AWS SQS Event Broker services with optional Dead-Letter Queue.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="queueUrl">Main SQS queue URL.</param>
        /// <param name="deadLetterQueueUrl">Optional DLQ URL.</param>
        public static IServiceCollection AddSQSEventBroker(this IServiceCollection services, string queueUrl, string? deadLetterQueueUrl = null)
        {
            services.AddAWSService<IAmazonSQS>();
            services.AddSingleton<IEventPublisher>(sp => new SqsEventPublisher(sp.GetRequiredService<IAmazonSQS>(), queueUrl));
            services.AddSingleton<IEventConsumer>(sp =>
            {
                var sqsClient = sp.GetRequiredService<IAmazonSQS>();
                var logger = sp.GetRequiredService<ILogger<SqsEventConsumer>>();
                return new SqsEventConsumer(logger, sqsClient, queueUrl, deadLetterQueueUrl);
            });
            return services;
        }


        //public static IServiceCollection AddInMemoryEventBroker(this IServiceCollection services)
        //{
        //    var queue = new ConcurrentQueue<string>();
        //    services.AddSingleton<IEventPublisher>(sp => new InMemoryEventPublisher(queue));
        //    services.AddSingleton<IEventConsumer>(sp => new InMemoryEventConsumer(queue));
        //    return services;
        //}
    }
}
