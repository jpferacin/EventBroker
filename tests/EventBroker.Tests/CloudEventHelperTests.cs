using SiriusPt.EventBroker.Core.Events;
using SiriusPt.EventBroker.Core.Helpers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace EventBroker.Core.Tests;

public class CloudEventHelperTests
{
    public class EventWithEventBridgeEnvelope
    {
        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;

        [JsonPropertyName("detail-type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("detail")]
        public object Detail { get; set; } = new { Id = 456, Name = "EventBridge-Test" };
    }

    [Fact]
    public void UnwrapPayload_ShouldReturnMessage_FromSNS()
    {
        // Arrange: SNS envelope with Message property
        var snsEnvelope = JsonSerializer.Serialize(new
        {
            Type = "Notification",
            Message = "{\"Id\":123,\"Name\":\"SNS-Test\"}"
        });

        // Act
        var result = CloudEventHelper.UnwrapPayload(snsEnvelope);

        // Assert
        Assert.Contains("\"Id\":123", result);
        Assert.Contains("\"Name\":\"SNS-Test\"", result);
    }

    [Fact]
    public void UnwrapPayload_ShouldReturnDetail_FromEventBridge()
    {
        // Arrange: EventBridge envelope with detail property
        var eventBridgeEnvelope = JsonSerializer.Serialize(new EventWithEventBridgeEnvelope
        {
            Source = "aws.events",
            Type = "TestEvent",
            Detail = new { Id = 456, Name = "EventBridge-Test" }
        });

        // Act
        var result = CloudEventHelper.UnwrapPayload(eventBridgeEnvelope);

        // Assert
        Assert.Contains("\"Id\":456", result);
        Assert.Contains("\"Name\":\"EventBridge-Test\"", result);
    }

    [Fact]
    public void UnwrapPayload_ShouldReturnRawBody_ForDirectSQS()
    {
        // Arrange: Direct SQS message
        var rawMessage = "{\"Id\":789,\"Name\":\"DirectSQS\"}";

        // Act
        var result = CloudEventHelper.UnwrapPayload(rawMessage);

        // Assert
        Assert.Equal(rawMessage, result);
    }

    [Fact]
    public void UnwrapPayload_ShouldReturnRawBody_WhenInvalidJson()
    {
        // Arrange: Invalid JSON
        var invalidJson = "Not a JSON string";

        // Act
        var result = CloudEventHelper.UnwrapPayload(invalidJson);

        // Assert
        Assert.Equal(invalidJson, result);
    }
}