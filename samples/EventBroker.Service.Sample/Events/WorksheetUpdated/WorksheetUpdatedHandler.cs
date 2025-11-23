using EventBroker.Service.Sample.Models;
using SiriusPt.EventBroker.Core;
using SiriusPt.EventBroker.Core.Events;
using SiriusPt.EventBroker.Core.Interfaces;

namespace EventBroker.Service.Sample.Events.WorksheetUpdated;

public partial class WorksheetUpdatedHandler : IEventHandler<CloudEvent<WorksheetUpdated>>
{
    private readonly ILogger<WorksheetUpdatedHandler> _logger;

    public WorksheetUpdatedHandler(ILogger<WorksheetUpdatedHandler> logger)
    {
        _logger = logger;
    }

    public async Task<EventProcessResult> HandleAsync(CloudEvent<WorksheetUpdated> request, CancellationToken cancellationToken)
    {
        try
        {
            var data = request.Data;
            LogProcessingStarted(request.Id);

            await Task.Delay(100, cancellationToken);
            LogProcessingSucceeded(request.Id);

            return OperationResult.Ok(request.Id);
        }
        catch (Exception ex)
        {
            LogProcessingFailed(request.Id, ex.Message);
            var error = (ex.InnerException ?? ex).Message;
            return new OperationResult(request.Id, error);
        }
    }
    [LoggerMessage(EventId = 600, Level = LogLevel.Information, Message = "Processing WorksheetUpdated for Id={Id}")]
    partial void LogProcessingStarted(string Id);
    [LoggerMessage(EventId = 601, Level = LogLevel.Information, Message = "WorksheetUpdated processed successfully for Id={Id}")]
    partial void LogProcessingSucceeded(string Id);
    [LoggerMessage(EventId = 602, Level = LogLevel.Error, Message = "Failed to process WorksheetUpdated for Id={Id}. Error={Error}")]
    partial void LogProcessingFailed(string Id, string Error);
}
