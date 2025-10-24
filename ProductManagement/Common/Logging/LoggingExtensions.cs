using Microsoft.Extensions.Logging;

namespace ProductManagement.Common.Logging;

public static class LoggingExtensions
{
    public static void LogProductCreationMetrics(this ILogger logger, LoggingModels.ProductCreationMetrics metrics)
    {
        var eventId = metrics.Success ? LoggingModels.LogEvents.ProductCreationCompleted : LoggingModels.LogEvents.ProductValidationFailed;

        logger.LogInformation(eventId,
            "Product creation {Status} - Operation: {OperationId}, Name: {ProductName}, SKU: {SKU}, Category: {Category}, " +
            "Validation: {ValidationMs}ms, Database: {DatabaseMs}ms, Total: {TotalMs}ms{ErrorDetails}",
            metrics.Success ? "COMPLETED" : "FAILED",
            metrics.OperationId,
            metrics.ProductName,
            metrics.SKU,
            metrics.Category,
            metrics.ValidationDuration.TotalMilliseconds,
            metrics.DatabaseSaveDuration.TotalMilliseconds,
            metrics.TotalDuration.TotalMilliseconds,
            !string.IsNullOrEmpty(metrics.ErrorReason) ? $", Error: {metrics.ErrorReason}" : string.Empty);
    }
}