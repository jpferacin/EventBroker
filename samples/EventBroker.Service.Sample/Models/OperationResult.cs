using SiriusPt.EventBroker.Core;
using System.Text.Json.Serialization;

namespace EventBroker.Service.Sample.Models;



public class OperationResult : EventProcessResult
{
    public OperationResult() : this("n/a", true, [])
    {
    }

    public OperationResult(string identifier) : this(identifier, true, [])
    {
    }

    public OperationResult(params string[] errors) : this("n/a", false, errors)
    {
    }

    public OperationResult(string identifier, params string[] errors) : this(identifier, false, errors)
    {
    }

    public OperationResult(string? identifier, bool success, string[] errors)
        : base(success, errors)
    {
        Identifier = string.IsNullOrWhiteSpace(identifier) ? "n/a" : identifier;
    }

    [JsonPropertyName("identifier")]
    public string Identifier { get; init; } = "n/a";

    // Convenience factory methods
    public static OperationResult Ok(string identifier) => new(identifier, true, []);
    public static OperationResult Fail(string identifier, params string[] errors) => new(identifier, false, errors);
}
