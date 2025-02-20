using NArchitecture.Core.Security.Abstractions.Authenticator;
using NArchitecture.Core.Security.Abstractions.Authenticator.Enums;

namespace NArchitecture.Core.Security.Authenticator;

public class DefaultAuthenticatorConfiguration : IAuthenticatorConfiguration
{
    public int CodeLength { get; init; } = 6;
    public int CodeSeedLength { get; init; } = 32;
    public TimeSpan CodeExpiration { get; init; } = TimeSpan.FromMinutes(3);

    private const string DefaultAuthenticatorNotFound = "No authenticator found for the user.";
    private const string DefaultCodeExpired = "Authentication code has expired.";
    private const string DefaultDestinationRequired = "Destination is required for email and SMS authentication.";
    private const string DefaultInvalidCode = "Invalid authentication code.";
    private const string DefaultUnsupportedType = "Authenticator type {0} is not supported.";
    private static readonly EmailTemplateConfiguration DefaultEmailTemplate = new()
    {
        Subject = "Authentication Code",
        TextBodyTemplate = "Your authentication code: {0}",
        HtmlBodyTemplate = "<h3>Your authentication code:</h3><br/><strong>{0}</strong>",
    };
    private static readonly SmsTemplateConfiguration DefaultSmsTemplate = new()
    {
        MessageTemplate = "Your authentication code: {0}",
    };

    public HashSet<AuthenticatorType> EnabledAuthenticatorTypes { get; init; } =
        [AuthenticatorType.Email, AuthenticatorType.Sms, AuthenticatorType.Otp];

    public virtual ValueTask<EmailTemplateConfiguration> GetEmailTemplateAsync(
        string code,
        CancellationToken cancellationToken = default
    )
    {
        return ValueTask.FromResult(DefaultEmailTemplate);
    }

    public virtual ValueTask<SmsTemplateConfiguration> GetSmsTemplateAsync(
        string code,
        CancellationToken cancellationToken = default
    )
    {
        return ValueTask.FromResult(DefaultSmsTemplate);
    }

    public virtual ValueTask<string> GetAuthenticatorNotFoundMessageAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(DefaultAuthenticatorNotFound);
    }

    public virtual ValueTask<string> GetCodeExpiredMessageAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(DefaultCodeExpired);
    }

    public virtual ValueTask<string> GetDestinationRequiredMessageAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(DefaultDestinationRequired);
    }

    public virtual ValueTask<string> GetInvalidCodeMessageAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(DefaultInvalidCode);
    }

    public virtual ValueTask<string> GetUnsupportedTypeMessageAsync(
        AuthenticatorType type,
        CancellationToken cancellationToken = default
    )
    {
        return ValueTask.FromResult(string.Format(DefaultUnsupportedType, type));
    }

    public virtual ValueTask<string> GetAuthenticatorTypeNotEnabledMessageAsync(
        AuthenticatorType type,
        CancellationToken cancellationToken = default
    )
    {
        return ValueTask.FromResult($"Authenticator type {type} is not enabled in the configuration.");
    }
}
