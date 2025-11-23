using SiriusPt.EventBroker.Core.Interfaces;
using SiriusPt.EventBroker.Core.Events;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace SiriusPt.EventBroker.Core;

/// <summary>
/// Maps the <see cref="IEventHandler{T}"/> to the type of message being processed.
/// </summary>
public class SubscriberMapping
{
    /// <inheritdoc/>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type HandlerType { get; }

    /// <inheritdoc/>
    public Type MessageType { get; }

    /// <inheritdoc/>
    public string MessageTypeIdentifier { get; }

    // The MessageEnvelopeFactory func is used to create func from the generic parameters
    // of the public interface where the types are known at compile time. This allows
    // Native AOT compatibility and avoid having to make reflection calls like Type.MakeGeneric
    // which is not compatible with Native AOT.
    internal Func<CloudEventEnvelope> MessageEnvelopeFactory { get; }

    /// <summary>
    /// Constructs an instance of <see cref="SubscriberMapping"/>
    /// </summary>
    /// <param name="handlerType">The type that implements <see cref="IEventHandler{T}"/></param>
    /// <param name="messageType">The type that will be message data will deserialized into</param>
    /// <param name="envelopeFactory">Func for creating <see cref="MessageEnvelope{messageType}"/></param>
    /// <param name="messageTypeIdentifier">Optional message type identifier. If not set the full name of the <see cref="MessageType"/> is used.</param>

    internal SubscriberMapping([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type handlerType, Type messageType, Func<CloudEventEnvelope> envelopeFactory, string? messageTypeIdentifier = null)
    {
        HandlerType = handlerType;
        MessageType = messageType;
        MessageTypeIdentifier =
            !string.IsNullOrEmpty(messageTypeIdentifier) ?
            messageTypeIdentifier :
            messageType.FullName ?? throw new InvalidDataContractException("Unable to retrieve the Full Name of the provided Message Type.");

        MessageEnvelopeFactory = envelopeFactory;
    }

    /// <summary>
    /// Creates a SubscriberMapping from the generic parameters for the handler and message.
    /// </summary>
    /// <typeparam name="THandler">The type that implements <see cref="IEventHandler{T}"/></typeparam>
    /// <typeparam name="TMessage">Func for creating <see cref="MessageEnvelope{messageType}"/></typeparam>
    /// <param name="messageTypeIdentifier"></param>
    /// <returns></returns>
    public static SubscriberMapping Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] THandler, TMessage>(string? messageTypeIdentifier = null)
        where THandler : IEventHandler<TMessage>
    {
        static CloudEvent<TMessage> envelopeFactory()
        {
            return new CloudEvent<TMessage>();
        }

        return new SubscriberMapping(typeof(THandler), typeof(TMessage), envelopeFactory, messageTypeIdentifier);
    }
}