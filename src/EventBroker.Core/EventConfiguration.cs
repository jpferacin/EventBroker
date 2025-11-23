using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SiriusPt.EventBroker.Core;

public class EventConfiguration
{
    public IList<SubscriberMapping> SubscriberMappings { get; } = new List<SubscriberMapping>();
    public JsonSerializerOptions SerializationOptions { get; } = new() { PropertyNameCaseInsensitive = true };
    public string? Source { get; set; }
    public bool LogMessageContent { get; set; }

    public SubscriberMapping? GetHandlerForMessage(Type messageType)
        => SubscriberMappings.FirstOrDefault(x => x.MessageType == messageType);

    public SubscriberMapping? GetHandlerForMessage(string messageTypeIdentifier)
        => SubscriberMappings.FirstOrDefault(x => x.MessageTypeIdentifier.Equals(messageTypeIdentifier, StringComparison.OrdinalIgnoreCase));
}
