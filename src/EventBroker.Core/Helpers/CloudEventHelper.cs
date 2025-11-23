using SiriusPt.EventBroker.Core.Events;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace SiriusPt.EventBroker.Core.Helpers;

public static class CloudEventHelper
{
    /// <summary>
    /// Creates a CloudEvent<T> with sensible defaults and allows overrides.
    /// </summary>
    public static CloudEvent<T> Create<T>(
        T data,
        string? source = null,
        string? type = null,
        string? id = null,
        DateTimeOffset? time = null,
        string? dataContentType = null,
        Dictionary<string, string>? attributes = null)
    {
        return new CloudEvent<T>
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Source = source ?? "urn:source:default",
            SpecVersion = "1.0",
            Type = type ?? typeof(T).Name,
            Time = time ?? DateTimeOffset.UtcNow,
            DataContentType = dataContentType ?? "application/json",
            Data = data,
            Attributes = attributes ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };
    }

    /// <summary>
    /// Attempts to parse and validate a CloudEvent from JSON.
    /// </summary>
    /// <param name="json">The raw JSON string.</param>
    /// <param name="cloudEvent">The parsed CloudEvent if valid.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool TryGetCloudEvent(string json, out CloudEvent<JsonElement> cloudEvent)
    {
        cloudEvent = null!;
        try
        {
            var parsed = JsonSerializer.Deserialize<CloudEvent<JsonElement>>(UnwrapPayload(json));
            if (parsed is null ||
                string.IsNullOrWhiteSpace(parsed.Id) ||
                string.IsNullOrWhiteSpace(parsed.Type) ||
                string.IsNullOrWhiteSpace(parsed.Source) ||
                parsed.Data.ValueKind == JsonValueKind.Undefined)
            {
                return false;
            }

            cloudEvent = parsed;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryGetEventType(string json, out string eventType)
    {
        eventType = string.Empty;

        // Try CloudEvent parsing
        if (TryGetCloudEvent(json, out var cloudEvent))
        {
            eventType = cloudEvent.Type;
            return !string.IsNullOrEmpty(eventType);
        }

        // Fallback: parse raw JSON for "type" property (case-insensitive)
        try
        {
            var normalizedJson = UnwrapPayload(json);
            using var doc = JsonDocument.Parse(normalizedJson);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in root.EnumerateObject())
                {
                    if (property.Name.Equals("type", StringComparison.OrdinalIgnoreCase))
                    {
                        eventType = property.Value.GetString() ?? string.Empty;
                        return !string.IsNullOrEmpty(eventType);
                    }
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    
    public static bool IsValidJson(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        try
        {
            JsonDocument.Parse(input);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }


/// <summary>
/// Extracts the actual payload from an SQS message body that may come from SNS or EventBridge.
/// </summary>
/// <param name="rawBody">The raw SQS message body.</param>
/// <returns>The unwrapped JSON payload as string.</returns>
public static string UnwrapPayload(string rawBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;

            // SNS envelope detection
            if (root.TryGetProperty("Type", out _) && root.TryGetProperty("Message", out var snsMessage))
            {
                return snsMessage.GetString() ?? rawBody;
            }

            // EventBridge envelope detection
            if (root.TryGetProperty("detail-type", out _) && root.TryGetProperty("detail", out var detail))
            {
                return detail.GetRawText();
            }

            // Direct SQS message
            return rawBody;
        }
        catch
        {
            // If parsing fails, return raw body
            return rawBody;
        }
    }

}