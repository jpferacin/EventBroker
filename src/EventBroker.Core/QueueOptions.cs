namespace SiriusPt.EventBroker.Core;

public class QueueOptions
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string DLQUrl { get; set; } = string.Empty;
}
