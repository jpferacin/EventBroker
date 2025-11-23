using System.Text.Json.Serialization;

namespace SiriusPt.EventBroker.Core;

public class EventProcessResult
{
    public EventProcessResult() : this(true, [])
    {
    }

    public EventProcessResult(string error) : this(false, [error])
    {
    }

    public EventProcessResult(bool success, string[] errors)
    {
        Success = success;
        Errors = errors ?? [];
    }

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("errors")]
    public string[] Errors { get; init; } = [];

    public string Error => Errors.Length > 0 ? Errors[0] : string.Empty;

    public static EventProcessResult Ok() => new(true, []);
    public static EventProcessResult Fail(params string[] errors) => new(false, errors);
}
