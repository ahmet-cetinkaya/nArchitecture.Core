using System.Text.Json;
using MediatR;
using NArchitecture.Core.CrossCuttingConcerns.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Logging.Abstractions;

namespace NArchitecture.Core.Application.Pipelines.Logging;

/// <summary>
/// Pipeline behavior that handles logging for requests implementing ILoggableRequest.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response being returned.</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ILoggableRequest
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Handles the request by logging its details before and after processing.
    /// </summary>
    /// <param name="request">The request to process.</param>
    /// <param name="next">The delegate for the next handler in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response from the request handler.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
            throw new NullReferenceException(nameof(request));

        const int InitialDictionaryCapacity = 8;
        using var requestDict = new ValueDictionary<string, object>(InitialDictionaryCapacity);

        foreach (System.Reflection.PropertyInfo prop in typeof(TRequest).GetProperties())
        {
            object? value = prop.GetValue(request);
            if (value is null)
                continue;

            LogExcludeParameter excludeParam = Array.Find(request.LogOptions.ExcludeParameters, p => p.Name == prop.Name);

            if (excludeParam.Name != prop.Name)
            {
                requestDict.Add(prop.Name, value);
                continue;
            }

            if (!excludeParam.Mask)
                continue;

            if (value is string strValue)
            {
                requestDict.Add(prop.Name, MaskValue(strValue, excludeParam));
            }
        }

        // Create log detail with proper method name and user
        LogDetail logDetail = new()
        {
            MethodName = "RequestHandlerDelegate", // Use fixed name instead of lambda name
            Parameters = [new LogParameter { Type = typeof(TRequest).Name, Value = requestDict }],
            User = request.LogOptions.User,
        };

        // Log request
        _ = logger.InformationAsync(JsonSerializer.Serialize(logDetail, _jsonOptions));

        // Execute handler
        TResponse? response = await next();

        // Log response if configured
        if (request.LogOptions.LogResponse)
        {
            logDetail.Parameters.Add(new LogParameter { Type = typeof(TResponse).Name, Value = response! });
            _ = logger.InformationAsync(JsonSerializer.Serialize(logDetail, _jsonOptions));
        }

        return response;
    }

    /// <summary>
    /// Masks sensitive values in strings based on the provided parameters.
    /// </summary>
    /// <param name="value">The string value to mask.</param>
    /// <param name="param">The parameters defining how to mask the value.</param>
    /// <returns>The masked string value.</returns>
    private static string MaskValue(scoped ReadOnlySpan<char> value, in LogExcludeParameter param)
    {
        if (value.IsEmpty)
            return string.Empty;

        if (value.Contains(new ReadOnlySpan<char>(['@']), StringComparison.Ordinal))
            return MaskEmail(value, param);
        else if (value.IsNumeric())
            return MaskNumeric(value, param);

        return param.KeepEndChars == 0 ? MaskWithFixedLength(value, param) : MaskDefault(value, param);
    }

    private static string MaskEmail(ReadOnlySpan<char> value, in LogExcludeParameter param)
    {
        if (value.Length < 9)
            return value.ToString();

        const int maskLength = 6;
        int startChars = param.Name == "SensitiveData" ? 4 : param.KeepStartChars;
        int endChars = param.Name == "SensitiveData" ? 5 : param.KeepEndChars;

        // Removed threshold check to always mask
        char[] emailMaskedResult = new char[startChars + maskLength + endChars];
        value[..startChars].CopyTo(emailMaskedResult);
        emailMaskedResult.AsSpan(startChars, maskLength).Fill(param.MaskChar);
        value[^endChars..].CopyTo(emailMaskedResult.AsSpan(startChars + maskLength));
        return new string(emailMaskedResult);
    }

    private static string MaskNumeric(ReadOnlySpan<char> value, in LogExcludeParameter param)
    {
        if (value.Length <= 4)
            return value.ToString();

        const int keepStart = 2,
            keepEnd = 2;
        char[] numericResult = new char[value.Length];
        value[..keepStart].CopyTo(numericResult);
        numericResult.AsSpan(keepStart, value.Length - keepStart - keepEnd).Fill(param.MaskChar);
        value[^keepEnd..].CopyTo(numericResult.AsSpan(value.Length - keepEnd));
        return new string(numericResult);
    }

    private static string MaskWithFixedLength(ReadOnlySpan<char> value, in LogExcludeParameter param)
    {
        if (value.Length <= param.KeepStartChars)
            return value.ToString();

        const int fixedMaskLength = 3;
        char[] maskedFixedLengthResult = new char[param.KeepStartChars + fixedMaskLength];
        value[..param.KeepStartChars].CopyTo(maskedFixedLengthResult);
        maskedFixedLengthResult.AsSpan(param.KeepStartChars).Fill(param.MaskChar);
        return new string(maskedFixedLengthResult);
    }

    private static string MaskDefault(ReadOnlySpan<char> value, in LogExcludeParameter param)
    {
        // If the string is exactly keepStartChars + keepEndChars + 1, mask exactly one middle char.
        if (value.Length == param.KeepStartChars + param.KeepEndChars + 1)
        {
            char[] simpleMaskedResult = new char[value.Length];
            value[..param.KeepStartChars].CopyTo(simpleMaskedResult);
            simpleMaskedResult[param.KeepStartChars] = param.MaskChar;
            value[^param.KeepEndChars..].CopyTo(simpleMaskedResult.AsSpan(param.KeepStartChars + 1));
            return new string(simpleMaskedResult);
        }

        if (value.Length <= param.KeepStartChars + param.KeepEndChars)
            return value.ToString();

        // For short strings, fill all positions between keepStart and (value.Length - keepEnd) with maskChar.
        if (value.Length < param.KeepStartChars + param.KeepEndChars + 5)
        {
            char[] simpleMaskedResult = new char[value.Length];
            value[..param.KeepStartChars].CopyTo(simpleMaskedResult);
            for (int i = param.KeepStartChars; i < value.Length - param.KeepEndChars; i++)
                simpleMaskedResult[i] = param.MaskChar;
            value[^param.KeepEndChars..].CopyTo(simpleMaskedResult.AsSpan(value.Length - param.KeepEndChars));
            return new string(simpleMaskedResult);
        }

        int maskLength = 5; // Fixed mask length for longer strings
        char[] longMaskedResult = new char[param.KeepStartChars + maskLength + param.KeepEndChars];
        value[..param.KeepStartChars].CopyTo(longMaskedResult);
        longMaskedResult.AsSpan(param.KeepStartChars, maskLength).Fill(param.MaskChar);
        value[^param.KeepEndChars..].CopyTo(longMaskedResult.AsSpan(param.KeepStartChars + maskLength));
        return new string(longMaskedResult);
    }
}

file class ValueDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IDisposable
    where TKey : notnull
{
    public ValueDictionary(int capacity)
        : base(capacity) { }

    public void Dispose() => Clear();
}

file static class SpanExtensions
{
    public static bool IsNumeric(this ReadOnlySpan<char> span)
    {
        foreach (char c in span)
            if (!char.IsDigit(c))
                return false;
        return true;
    }
}
