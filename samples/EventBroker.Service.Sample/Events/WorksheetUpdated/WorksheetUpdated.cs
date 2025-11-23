using MediatR;

namespace EventBroker.Service.Sample.Events.WorksheetUpdated;

public class WorksheetUpdated
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; init; } = nameof(WorksheetUpdated);
}
