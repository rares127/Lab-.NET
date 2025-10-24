using System.Diagnostics;

namespace ProductManagement.Common.Middleware;

public class CorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationMiddleware> _logger;
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    public CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        
        // Set correlation ID in response header
        context.Response.Headers.Append(CorrelationIdHeaderName, correlationId);
        
        // Set correlation ID in Activity for distributed tracing
        Activity.Current?.SetTag("correlation.id", correlationId);
        
        // Add correlation ID to HttpContext items for easy access
        context.Items["CorrelationId"] = correlationId;

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            _logger.LogInformation("Processing request with correlation ID: {CorrelationId}", correlationId);
            
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Request failed with correlation ID: {CorrelationId}", correlationId);
                throw;
            }
            finally
            {
                _logger.LogInformation("Completed request with correlation ID: {CorrelationId}", correlationId);
            }
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        // Check if correlation ID exists in request headers
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId) && 
            !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.ToString();
        }

        // Generate new correlation ID if not present
        return Guid.NewGuid().ToString();
    }
}
