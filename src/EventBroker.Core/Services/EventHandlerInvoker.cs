
using SiriusPt.EventBroker.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SiriusPt.EventBroker.Core.Services;

public partial class EventHandlerInvoker : IEventHandlerInvoker
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventHandlerInvoker> _logger;

    private readonly ConcurrentDictionary<Type, MethodInfo?> _handlerMethods = new();

    public EventHandlerInvoker(IServiceProvider serviceProvider, ILogger<EventHandlerInvoker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    private static string TryGetIdValue(object obj)
    {
        if (obj is null)
        {
            return string.Empty;
        }

        var property = obj.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(obj)?.ToString() ?? string.Empty;
    }

    public async Task<EventProcessResult> InvokeHandlerAsync(SubscriberMapping mapping, object typedEvent, CancellationToken token = default)
    {
        var eventId = TryGetIdValue(typedEvent);

        try
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetService(mapping.HandlerType);
            if (handler is null)
            {
                LogHandlerNotResolved(mapping.HandlerType, eventId);
                return EventProcessResult.Fail("Handler not resolved");
            }
;
            var method = _handlerMethods.GetOrAdd(mapping.MessageType, _ =>
                mapping.HandlerType.GetMethod(nameof(IEventHandler<EventProcessResult>.HandleAsync), [mapping.MessageType, typeof(CancellationToken)]));
            if (method is null)
            {
                LogHandleAsyncNotFound(mapping.HandlerType, eventId);
                return EventProcessResult.Fail("Method not resolved");
            }

            try
            {
                if (method.Invoke(handler, [typedEvent, token]) is Task<EventProcessResult> task)
                {
                    return await task;
                }
                else
                {
                    LogUnexpectedReturnType(mapping.HandlerType, eventId, nameof(Task<EventProcessResult>));
                    return EventProcessResult.Fail("Unexpected return type");
                }
            }
            catch (TargetInvocationException ex)
            {
                LogHandlerException(ex.InnerException ?? ex, mapping.HandlerType, eventId);
                return EventProcessResult.Fail("Handler invocation error");
            }
            catch (Exception ex)
            {
                LogUnexpectedException(ex, mapping.HandlerType, eventId);
                return EventProcessResult.Fail("Unexpected handler error", ex.Message);
            }
        }
        catch (Exception ex)
        {
            LogCriticalError(ex, eventId);
            return EventProcessResult.Fail("Critical error", ex.Message);
        }
    }

    [LoggerMessage(EventId = 2000, Level = LogLevel.Warning, Message = "Handler of type {HandlerType} could not be resolved for event ID {EventId}.")]
    partial void LogHandlerNotResolved(Type HandlerType, string EventId);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Warning, Message = "No compatible HandleAsync method found on handler {HandlerType} for event ID {EventId}.")]
    partial void LogHandleAsyncNotFound(Type HandlerType, string EventId);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Error, Message = "Unexpected return type from HandleAsync on handler {HandlerType} for event ID {EventId}. Expected {ExpectedType}.")]
    partial void LogUnexpectedReturnType(Type HandlerType, string EventId, string ExpectedType);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Error, Message = "Exception occurred while invoking handler {HandlerType} for event ID {EventId}.")]
    partial void LogHandlerException(Exception ex, Type HandlerType, string EventId);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Error, Message = "Unexpected exception occurred while invoking handler {HandlerType} for event ID {EventId}.")]
    partial void LogUnexpectedException(Exception ex, Type HandlerType, string EventId);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Critical, Message = "Critical error occurred while processing event ID {EventId}.")]
    partial void LogCriticalError(Exception ex, string EventId);
}
