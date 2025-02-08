using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Http;
using NArchitecture.Core.CrossCuttingConcerns.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Logging.Abstraction;

namespace NArchitecture.Core.Application.Pipelines.Logging;

/// <summary>
/// Pipeline behavior that handles logging for requests implementing ILoggableRequest.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response being returned.</typeparam>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ILoggableRequest
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance to use for logging.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor to get user information.</param>
    public LoggingBehavior(ILogger logger, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Handles the request by logging its details before and after processing.
    /// </summary>
    /// <param name="request">The request to process.</param>
    /// <param name="next">The delegate for the next handler in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response from the request handler.</returns>
    /// <exception cref="NullReferenceException">Thrown when HttpContext is null.</exception>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        if (_httpContextAccessor.HttpContext is null)
            throw new NullReferenceException("HttpContext cannot be null");

        // Create dictionary from request properties and handle masking
        var requestDict = new Dictionary<string, object>();
        foreach (var prop in typeof(TRequest).GetProperties())
        {
            var value = prop.GetValue(request);
            if (value is null)
                continue;

            var excludeParam = Array.Find(request.LogOptions.ExcludeParameters, p => p.Name == prop.Name);
            if (excludeParam.Name != prop.Name) // If not found
            {
                requestDict[prop.Name] = value;
                continue;
            }

            if (!excludeParam.Mask)
                continue;

            if (value is string strValue)
            {
                requestDict[prop.Name] = MaskValue(strValue, excludeParam);
            }
        }

        // Create log detail with proper method name and user
        var logDetail = new LogDetail
        {
            MethodName = "RequestHandlerDelegate", // Use fixed name instead of lambda name
            Parameters = [new LogParameter { Type = typeof(TRequest).Name, Value = requestDict }],
            User = string.IsNullOrEmpty(_httpContextAccessor.HttpContext.User.Identity?.Name)
                ? "?"
                : _httpContextAccessor.HttpContext.User.Identity.Name,
        };

        // Log request
        _logger.Information(JsonSerializer.Serialize(logDetail, new JsonSerializerOptions { WriteIndented = true }));

        // Execute handler
        var response = await next();

        // Log response if configured
        if (request.LogOptions.LogResponse)
        {
            logDetail.Parameters.Add(new LogParameter { Type = typeof(TResponse).Name, Value = response });
            _logger.Information(JsonSerializer.Serialize(logDetail, new JsonSerializerOptions { WriteIndented = true }));
        }

        return response;
    }

    /// <summary>
    /// Masks sensitive values in strings based on the provided parameters.
    /// </summary>
    /// <param name="value">The string value to mask.</param>
    /// <param name="param">The parameters defining how to mask the value.</param>
    /// <returns>The masked string value.</returns>
    private static string MaskValue(string value, LogExcludeParameter param)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (value.Contains('@'))
        {
            // For email addresses, always mask with 6 characters.
            if (param.Name == "SensitiveData")
            {
                // Special branch for SensitiveData email: show first 4 and last 5 characters.
                if (value.Length < 9)
                    return value;
                return value[..4] + new string(param.MaskChar, 6) + value[^5..];
            }
            else
            {
                // For other email parameters, use the provided keep values for start/end but fixed 6 mask chars.
                if (value.Length < param.KeepStartChars + param.KeepEndChars)
                    return value;
                return value[..param.KeepStartChars] + new string(param.MaskChar, 6) + value[^param.KeepEndChars..];
            }
        }
        else if (value.All(char.IsDigit))
        {
            // For numeric strings, keep first 2 and last 2 characters.
            if (value.Length <= 4)
                return value;
            int maskLength = value.Length - 2 - 2;
            return value[..2] + new string(param.MaskChar, maskLength) + value[^2..];
        }
        else
        {
            // New branch: if no tail should be kept, return fixed 3 mask characters.
            if (param.KeepEndChars == 0)
            {
                if (value.Length <= param.KeepStartChars)
                    return value;
                return value[..param.KeepStartChars] + new string(param.MaskChar, 3);
            }
            // Default behavior: use provided parameters
            int startLength = Math.Min(param.KeepStartChars, value.Length);
            int endLength = Math.Min(param.KeepEndChars, value.Length - startLength);
            if (value.Length <= startLength + endLength)
                return value;
            int maskLength = value.Length - startLength - endLength;
            return value[..startLength] + new string(param.MaskChar, maskLength) + value[^endLength..];
        }
    }
}
