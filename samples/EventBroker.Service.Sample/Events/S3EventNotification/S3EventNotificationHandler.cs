using Amazon.S3.Util;
using EventBroker.Service.Sample.Models;
using SiriusPt.EventBroker.Core;
using SiriusPt.EventBroker.Core.Interfaces;

namespace EventBroker.Service.Sample.Events.WorksheetUpdated;

public partial class S3EventNotificationHandler : IEventHandler<S3EventNotification>
{
    private readonly ILogger<S3EventNotificationHandler> _logger;

    public S3EventNotificationHandler(ILogger<S3EventNotificationHandler> logger)
    {
        _logger = logger;
    }

    public async Task<EventProcessResult> HandleAsync(S3EventNotification request, CancellationToken cancellationToken)
    {
        try
        {
            foreach(var record in request.Records)
            {
                LogProcessingStarted(record.S3.Object.Key);

                await Task.Delay(100, cancellationToken);
                LogProcessingSucceeded(record.S3.Object.Key);
            }

            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            LogProcessingFailed("...", ex.Message);
            var error = (ex.InnerException ?? ex).Message;
            return OperationResult.Fail("...", error);
        }
    }
    [LoggerMessage(EventId = 600, Level = LogLevel.Information, Message = "Processing WorksheetUpdated for Id={Id}")]
    partial void LogProcessingStarted(string Id);
    [LoggerMessage(EventId = 601, Level = LogLevel.Information, Message = "WorksheetUpdated processed successfully for Id={Id}")]
    partial void LogProcessingSucceeded(string Id);
    [LoggerMessage(EventId = 602, Level = LogLevel.Error, Message = "Failed to process WorksheetUpdated for Id={Id}. Error={Error}")]
    partial void LogProcessingFailed(string Id, string Error);
}