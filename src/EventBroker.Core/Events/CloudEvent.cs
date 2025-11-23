using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SiriusPt.EventBroker.Core.Events;

public class CloudEventEnvelope
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = Guid.NewGuid().ToString();

    [JsonPropertyName("source")]
    public string Source { get; init; } = string.Empty;

    [JsonPropertyName("specversion")]
    public string SpecVersion { get; init; } = "1.0";

    [JsonPropertyName("type")]
    public virtual string Type { get; set; } = null!;

    [JsonPropertyName("time")]
    public DateTimeOffset Time { get; init; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("datacontenttype")]
    public string DataContentType { get; init; } = "application/json";


    [JsonPropertyName("attributes")]
    public Dictionary<string, string> Attributes { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class CloudEvent<TData> : CloudEventEnvelope
{
    [JsonPropertyName("data")]
    public TData Data { get; set; } = default!;

    [JsonPropertyName("type")]
    public override string Type { get; set; } = typeof(TData).Name;

    public CloudEvent()
    {
    }

    public CloudEvent(TData data)
    {
        Data = data;
        Type = typeof(TData).Name;
    }

    /// <summary>
    /// Attaches the user specified application message to the <see cref="CloudEvent{T}.Message"/> property.
    /// </summary>
    /// <param name="message">The user specified application message.</param>
    internal void SetMessage(object message)
    {
        Data = (TData)message;
    }

    /// <inheritdoc/>
    public override string ToString() => Id;
}

