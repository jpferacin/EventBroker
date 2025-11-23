using SiriusPt.EventBroker.Core.Interfaces;
using SiriusPt.EventBroker.Core.Events;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace SiriusPt.EventBroker.Core;

public class EventConfigurationBuilder : IEventConfigurationBuilder
{
    private readonly EventConfiguration _messageConfiguration = new();

    public IEventConfigurationBuilder WithMessageHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] THandler, TMessage>
        (string? messageTypeIdentifier = null)
        where THandler : IEventHandler<TMessage>
    {
        return WithMessageHandler(typeof(THandler), typeof(TMessage), () => new CloudEvent<TMessage>(), messageTypeIdentifier);
    }

    private IEventConfigurationBuilder WithMessageHandler(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type handlerType,
        Type messageType,
        Func<CloudEventEnvelope> envelopeFactory,
        string? messageTypeIdentifier = null)
    {
        var subscriberMapping = new SubscriberMapping(handlerType, messageType, envelopeFactory, messageTypeIdentifier);
        _messageConfiguration.SubscriberMappings.Add(subscriberMapping);
        return this;
    }

    public IEventConfigurationBuilder RegisterSubscribers(Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();
        var handlerInterface = typeof(IEventHandler<>);

        var handlerTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface)
                .Select(i => new { HandlerType = t, MessageType = i.GetGenericArguments()[0] }));

        foreach (var handler in handlerTypes)
        {
            WithMessageHandler(
                handler.HandlerType,
                handler.MessageType,
                () => (CloudEventEnvelope)Activator.CreateInstance(typeof(CloudEvent<>).MakeGenericType(handler.MessageType))!,
                GetFriendlyTypeName(handler.MessageType)
            );
        }

        return this;
    }

    private static string GetFriendlyTypeName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var genericTypeName = type.Name[..type.Name.IndexOf('`')]; // "CloudEvent"
        var genericArgs = type.GetGenericArguments()
                              .Select(t => t.Name); // ["WorksheetUpdated"]

        return $"{genericTypeName}<{string.Join(", ", genericArgs)}>";
    }

    public IEventConfigurationBuilder WithSerializationOptions(Action<JsonSerializerOptions> options)
    {
        options(_messageConfiguration.SerializationOptions);
        return this;
    }

    public IEventConfigurationBuilder WithMessageSource(string messageSource)
    {
        _messageConfiguration.Source = messageSource;
        return this;
    }

    public IEventConfigurationBuilder WithMessageContentLogging(bool value)
    {
        _messageConfiguration.LogMessageContent = value;
        return this;
    }

    public EventConfiguration Build() => _messageConfiguration;
}