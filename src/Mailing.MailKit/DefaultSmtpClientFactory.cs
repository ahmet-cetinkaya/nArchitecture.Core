using MailKit.Net.Smtp;

namespace NArchitecture.Core.Mailing.MailKit;

/// <summary>
/// Default implementation of <see cref="ISmtpClientFactory"/> that creates MailKit SMTP clients.
/// </summary>
public class DefaultSmtpClientFactory : ISmtpClientFactory
{
    /// <inheritdoc/>
    public ISmtpClient Create()
    {
        return new SmtpClient();
    }
}
