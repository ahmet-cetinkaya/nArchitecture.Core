using MailKit.Net.Smtp;

namespace NArchitecture.Core.Mailing.MailKit;

/// <summary>
/// Factory interface for creating SMTP clients.
/// </summary>
public interface ISmtpClientFactory
{
    /// <summary>
    /// Creates a new instance of an SMTP client.
    /// </summary>
    /// <returns>A new SMTP client instance.</returns>
    ISmtpClient Create();
}
