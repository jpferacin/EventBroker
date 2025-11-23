using MediatR;
using Microsoft.VisualBasic;
using SiriusPt.EventBroker.Core.Events;

namespace EventBroker.Service.Sample.Events.WorksheetCreated;

public class WorksheetCreated 
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = nameof(WorksheetCreated);
}
