using System.Text.Json;
using SiriusPt.EventBroker.Transport.SQS.Helpers;
using Xunit;

namespace SiriusPt.EventBroker.Tests;

public class MessageEnvelopeHelperTests
{
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
        var result = MessageEnvelopeHelper.UnwrapPayload(snsEnvelope);

        // Assert
        Assert.Contains("\"Id\":123", result);
        Assert.Contains("\"Name\":\"SNS-Test\"", result);
    }

    [Fact]
    public void UnwrapPayload_ShouldReturnDetail_FromEventBridge()
    {
        // Arrange: EventBridge envelope with detail property
        var eventBridgeEnvelope = JsonSerializer.Serialize(new
        {
            source = "aws.events",
            ["detail-type"] = "TestEvent",
            detail = new { Id = 456, Name = "EventBridge-Test" }
        });

        // Act
        var result = MessageEnvelopeHelper.UnwrapPayload(eventBridgeEnvelope);

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
        var result = MessageEnvelopeHelper.UnwrapPayload(rawMessage);

        // Assert
        Assert.Equal(rawMessage, result);
    }

    [Fact]
    public void UnwrapPayload_ShouldReturnRawBody_WhenInvalidJson()
    {
        // Arrange: Invalid JSON
        var invalidJson = "Not a JSON string";

        // Act
        var result = MessageEnvelopeHelper.UnwrapPayload(invalidJson);

        // Assert
        Assert.Equal(invalidJson, result);
    }
}