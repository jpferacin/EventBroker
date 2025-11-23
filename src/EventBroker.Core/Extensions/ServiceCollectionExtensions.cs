using SiriusPt.EventBroker.Core.Interfaces;
using SiriusPt.EventBroker.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System;


namespace SiriusPt.EventBroker.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEventCore(this IServiceCollection services, Action<IEventConfigurationBuilder> configure)
    {
        var builder = new EventConfigurationBuilder();

        configure(builder);

        var config = builder.Build();
        services.AddSingleton(config);

        foreach (var mapping in config.SubscriberMappings)
        {
            services.AddScoped(mapping.HandlerType);
        }

        services.AddSingleton<IEventHandlerInvoker, EventHandlerInvoker>();
        return services;
    }

}