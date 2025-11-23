using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;

namespace SiriusPt.EventBroker.Core.Interfaces;

public interface IEventConfigurationBuilder
{
    IEventConfigurationBuilder RegisterSubscribers(Assembly? assembly = null);
    IEventConfigurationBuilder WithMessageContentLogging(bool value);
    IEventConfigurationBuilder WithMessageHandler<[DynamicallyAccessedMembers((DynamicallyAccessedMemberTypes)(-1))] THandler, TMessage>(string? messageTypeIdentifier = null)
        where THandler : IEventHandler<TMessage>;
    IEventConfigurationBuilder WithMessageSource(string messageSource);
    IEventConfigurationBuilder WithSerializationOptions(Action<JsonSerializerOptions> options);
}
