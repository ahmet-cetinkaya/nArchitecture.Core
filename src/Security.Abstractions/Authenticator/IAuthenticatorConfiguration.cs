using NArchitecture.Core.Security.Abstractions.Authenticator.Enums;

namespace NArchitecture.Core.Security.Abstractions.Authenticator;

/// <summary>
/// Defines the configuration contract for authentication operations.
/// </summary>
public interface IAuthenticatorConfiguration
{
    /// <summary>
    /// Gets the length of the authentication code.
    /// </summary>
    int CodeLength { get; }

    /// <summary>
    /// Gets the length of the code seed used for generating authentication codes.
    /// </summary>
    int CodeSeedLength { get; }

    /// <summary>
    /// Gets the duration for which the authentication code remains valid.
    /// </summary>
    TimeSpan CodeExpiration { get; }

    /// <summary>
    /// Gets the set of enabled authenticator types.
    /// </summary>
    HashSet<AuthenticatorType> EnabledAuthenticatorTypes { get; }

    /// <summary>
    /// Gets the email template configuration for the specified authentication code.
    /// </summary>
    ValueTask<EmailTemplateConfiguration> GetEmailTemplateAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the SMS template configuration for the specified authentication code.
    /// </summary>
    ValueTask<SmsTemplateConfiguration> GetSmsTemplateAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the error message when no authenticator is found for a user.
    /// </summary>
    ValueTask<string> GetAuthenticatorNotFoundMessageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the error message when the authentication code has expired.
    /// </summary>
    ValueTask<string> GetCodeExpiredMessageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the error message when a destination is required but not provided.
    /// </summary>
    ValueTask<string> GetDestinationRequiredMessageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the error message when an invalid authentication code is provided.
    /// </summary>
    ValueTask<string> GetInvalidCodeMessageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the error message when an unsupported authenticator type is used.
    /// </summary>
    ValueTask<string> GetUnsupportedTypeMessageAsync(AuthenticatorType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the error message when an authenticator type is not enabled in the configuration.
    /// </summary>
    ValueTask<string> GetAuthenticatorTypeNotEnabledMessageAsync(
        AuthenticatorType type,
        CancellationToken cancellationToken = default
    );
}

public readonly record struct EmailTemplateConfiguration(string Subject, string TextBodyTemplate, string HtmlBodyTemplate);

public readonly record struct SmsTemplateConfiguration(string MessageTemplate);
