using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Mailing.Abstractions;
using NArchitecture.Core.Security.Abstractions.Authenticator;
using NArchitecture.Core.Security.Abstractions.Authenticator.Entities;
using NArchitecture.Core.Security.Abstractions.Authenticator.Enums;
using NArchitecture.Core.Security.Abstractions.Authenticator.Otp;
using NArchitecture.Core.Security.Abstractions.Cryptography.Generation;
using NArchitecture.Core.Sms.Abstractions;

namespace NArchitecture.Core.Security.Authenticator;

public class Authenticator<TUserId, TUserAuthenticatorId>(
    IUserAuthenticatorRepository<TUserId, TUserAuthenticatorId> userAuthenticatorRepository,
    ICodeGenerator codeGenerator,
    IAuthenticatorConfiguration configuration,
    IMailService? mailService = null,
    ISmsService? smsService = null,
    IOtpService? otpAuthenticator = null
) : IAuthenticator<TUserId, TUserAuthenticatorId>
{
    public async Task<UserAuthenticator<TUserAuthenticatorId, TUserId>> CreateAsync(
        TUserId userId,
        AuthenticatorType type,
        string? destination,
        CancellationToken cancellationToken = default
    )
    {
        await ValidateAuthenticatorType(type, cancellationToken);

        UserAuthenticator<TUserAuthenticatorId, TUserId> authenticator = new(userId, type)
        {
            CodeExpiresAt = DateTime.UtcNow.Add(configuration.CodeExpiration),
            CodeSeed =
                type == AuthenticatorType.Otp
                    ? otpAuthenticator!.GenerateSecretKey([])
                    : Convert.FromBase64String(codeGenerator.GenerateBase64(configuration.CodeSeedLength)),
        };
        authenticator.Code = type switch
        {
            AuthenticatorType.Email or AuthenticatorType.Sms => codeGenerator.GenerateNumeric(
                configuration.CodeLength,
                authenticator.CodeSeed
            ),
            AuthenticatorType.Otp => null,
            _ => throw new NotSupportedException(await configuration.GetUnsupportedTypeMessageAsync(type, cancellationToken)),
        };

        _ = await userAuthenticatorRepository.AddAsync(authenticator, cancellationToken);
        return authenticator;
    }

    public async Task AttemptAsync(TUserId userId, string? destination, CancellationToken cancellationToken = default)
    {
        UserAuthenticator<TUserAuthenticatorId, TUserId> authenticator =
            await userAuthenticatorRepository.GetAsync(ua => ua.Id!.Equals(userId), cancellationToken: cancellationToken)
            ?? throw new BusinessException(await configuration.GetAuthenticatorNotFoundMessageAsync(cancellationToken));

        await ValidateAuthenticatorType(authenticator.Type, cancellationToken);

        if (authenticator.CodeExpiresAt < DateTime.UtcNow)
            throw new BusinessException(await configuration.GetCodeExpiredMessageAsync(cancellationToken));

        if (string.IsNullOrEmpty(destination) && authenticator.Type != AuthenticatorType.Otp)
            throw new BusinessException(await configuration.GetDestinationRequiredMessageAsync(cancellationToken));

        // Regenerate code using the same seed for verification
        if (authenticator.Type is AuthenticatorType.Email or AuthenticatorType.Sms)
        {
            authenticator.Code = codeGenerator.GenerateNumeric(configuration.CodeLength, authenticator.CodeSeed);
            _ = await userAuthenticatorRepository.UpdateAsync(authenticator, cancellationToken);
        }

        switch (authenticator.Type)
        {
            case AuthenticatorType.Email:
                EmailTemplateConfiguration emailTemplate = await configuration.GetEmailTemplateAsync(
                    authenticator.Code!,
                    cancellationToken
                );
                var mail = new Mail(
                    Subject: emailTemplate.Subject,
                    TextBody: string.Format(emailTemplate.TextBodyTemplate, authenticator.Code),
                    HtmlBody: string.Format(emailTemplate.HtmlBodyTemplate, authenticator.Code),
                    ToList: [new(destination, destination!)]
                )
                {
                    Priority = 1,
                };
                await mailService!.SendAsync(mail, cancellationToken);
                break;

            case AuthenticatorType.Sms:
                SmsTemplateConfiguration smsTemplate = await configuration.GetSmsTemplateAsync(
                    authenticator.Code!,
                    cancellationToken
                );
                Sms.Abstractions.Sms sms = new(
                    PhoneNumber: destination!,
                    Content: string.Format(smsTemplate.MessageTemplate, authenticator.Code)
                )
                {
                    Priority = 1,
                    CustomParameters = new()
                    {
                        ["type"] = "authentication",
                        ["expiresAt"] = authenticator.CodeExpiresAt?.ToString("O") ?? string.Empty,
                    },
                };
                await smsService!.SendAsync(sms, cancellationToken);
                break;

            case AuthenticatorType.Otp:
                break;

            default:
                throw new InvalidOperationException(
                    await configuration.GetUnsupportedTypeMessageAsync(authenticator.Type, cancellationToken)
                );
        }
    }

    protected virtual async Task ValidateAuthenticatorType(AuthenticatorType type, CancellationToken cancellationToken = default)
    {
        if (!configuration.EnabledAuthenticatorTypes.Contains(type))
            throw new BusinessException(await configuration.GetAuthenticatorTypeNotEnabledMessageAsync(type, cancellationToken));

        switch (type)
        {
            case AuthenticatorType.Email when mailService is null:
                throw new InvalidOperationException(
                    "Email authentication is enabled but no implementation of IMailService has been configured. "
                        + "Either register an implementation of IMailService or disable email authentication in the configuration."
                );
            case AuthenticatorType.Sms when smsService is null:
                throw new InvalidOperationException(
                    "SMS authentication is enabled but no implementation of ISmsService has been configured. "
                        + "Either register an implementation of ISmsService or disable SMS authentication in the configuration."
                );
            case AuthenticatorType.Otp when otpAuthenticator is null:
                throw new InvalidOperationException(
                    "OTP authentication is enabled but no implementation of IOtpService has been configured. "
                        + "Either register an implementation of IOtpService or disable OTP authentication in the configuration."
                );
        }
    }

    public async Task VerifyAsync(TUserId userId, string code, CancellationToken cancellationToken = default)
    {
        UserAuthenticator<TUserAuthenticatorId, TUserId> authenticator =
            await userAuthenticatorRepository.GetAsync(ua => ua.Id!.Equals(userId), cancellationToken: cancellationToken)
            ?? throw new BusinessException(await configuration.GetAuthenticatorNotFoundMessageAsync(cancellationToken));

        await ValidateAuthenticatorType(authenticator.Type, cancellationToken);
        if (authenticator.CodeExpiresAt != null && authenticator.CodeExpiresAt < DateTime.UtcNow)
            throw new BusinessException(await configuration.GetCodeExpiredMessageAsync(cancellationToken));

        bool isValid = authenticator.Type switch
        {
            AuthenticatorType.Email or AuthenticatorType.Sms => authenticator.Code == code,
            AuthenticatorType.Otp => otpAuthenticator!.ComputeOtp(authenticator.CodeSeed!) == code,
            _ => throw new NotSupportedException(
                await configuration.GetUnsupportedTypeMessageAsync(authenticator.Type, cancellationToken)
            ),
        };
        if (!isValid)
            throw new BusinessException(await configuration.GetInvalidCodeMessageAsync(cancellationToken));

        if (!authenticator.IsVerified)
        {
            authenticator.IsVerified = true;
            _ = await userAuthenticatorRepository.UpdateAsync(authenticator, cancellationToken: cancellationToken);
        }
    }

    public async Task DeleteAsync(TUserId userId, CancellationToken cancellationToken = default)
    {
        UserAuthenticator<TUserAuthenticatorId, TUserId>? authenticator = await userAuthenticatorRepository.GetAsync(
            ua => ua.Id!.Equals(userId),
            cancellationToken: cancellationToken
        );
        if (authenticator is not null)
            _ = await userAuthenticatorRepository.DeleteAsync(authenticator, cancellationToken: cancellationToken);
    }
}
