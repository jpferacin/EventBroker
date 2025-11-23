using EventBroker.Service.Sample.Models;
using SiriusPt.EventBroker.Core;
using SiriusPt.EventBroker.Core.Interfaces;

namespace EventBroker.Service.Sample.Events.WorksheetCreated;

public partial class WorksheetCreatedHandler : IEventHandler<WorksheetCreated>
{
    private readonly ILogger<WorksheetCreatedHandler> _logger;

    public WorksheetCreatedHandler(ILogger<WorksheetCreatedHandler> logger)
    {
        _logger = logger;
    }

    public async Task<EventProcessResult> HandleAsync(WorksheetCreated request, CancellationToken cancellationToken)
    {
        try
        {
            LogProcessingStarted(_logger, request.Id);
            // Simulate business logic
            await Task.Delay(100, cancellationToken);
            LogProcessingSucceeded(_logger, request.Id);
            return new OperationResult(request.Id);
        }
        catch (Exception ex)
        {

            LogProcessingFailed(_logger, request.Id, ex.Message);
            var error = (ex.InnerException ?? ex).Message;
            return new OperationResult(request.Id, error);
        }
    }

    [LoggerMessage(EventId = 500, Level = LogLevel.Information, Message = "Processing WorksheetCreated for Id={Id}")]
    static partial void LogProcessingStarted(ILogger logger, string Id);

    [LoggerMessage(EventId = 501, Level = LogLevel.Information, Message = "WorksheetCreated processed successfully for Id={Id}")]
    static partial void LogProcessingSucceeded(ILogger logger, string Id);

    [LoggerMessage(EventId = 502, Level = LogLevel.Error, Message = "Failed to process WorksheetCreated for Id={Id}. Error={Error}")]
    static partial void LogProcessingFailed(ILogger logger, string Id, string Error);
}