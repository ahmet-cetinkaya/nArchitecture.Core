using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Cryptography;
using NArchitecture.Core.Mailing.Abstractions;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;

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

/// <summary>
/// Default implementation of <see cref="ISmtpClientFactory"/> that creates MailKit SMTP clients.
/// </summary>
public class DefaultSmtpClientFactory : ISmtpClientFactory
{
    /// <inheritdoc/>
    public ISmtpClient Create() => new SmtpClient();
}

/// <summary>
/// Implements email sending functionality using MailKit library.
/// </summary>
public class MailKitMailService : IMailService
{
    private readonly MailSettings _mailSettings;
    private readonly ISmtpClientFactory _smtpClientFactory;

    public MailKitMailService(MailSettings configuration, ISmtpClientFactory? smtpClientFactory = null)
    {
        _mailSettings = configuration;
        _smtpClientFactory = smtpClientFactory ?? new DefaultSmtpClientFactory();
    }

    /// <inheritdoc/>
    public async Task SendEmailAsync(Mail mail, CancellationToken cancellationToken = default)
    {
        if (mail.ToList is null || mail.ToList.Count == 0)
            return;
        (MimeMessage email, ISmtpClient smtp) = await emailPrepareAsync(mail, cancellationToken);
        try
        {
            _ = await smtp.SendAsync(email, cancellationToken);
        }
        finally
        {
            await smtp.DisconnectAsync(true, cancellationToken);
            email.Dispose();
            smtp.Dispose();
        }
    }

    /// <summary>
    /// Prepares email message with all necessary headers, body and attachments.
    /// </summary>
    /// <param name="mail">The mail to be prepared.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Tuple containing prepared MimeMessage and configured SMTP client.</returns>
    private async Task<(MimeMessage email, ISmtpClient smtp)> emailPrepareAsync(Mail mail, CancellationToken cancellationToken)
    {
        MimeMessage email = new();
        ISmtpClient smtp = _smtpClientFactory.Create();

        try
        {
            // Configure basic email properties
            email.From.Add(new MailboxAddress(_mailSettings.SenderFullName, _mailSettings.SenderEmail));
            email.To.AddRange(mail.ToList);

            // Add optional recipients
            if (mail.CcList?.Count > 0)
                email.Cc.AddRange(mail.CcList);
            if (mail.BccList?.Count > 0)
                email.Bcc.AddRange(mail.BccList);
            if (mail.ReplyTo?.Count > 0)
                email.ReplyTo.AddRange(mail.ReplyTo);

            email.Subject = mail.Subject;

            // Add custom headers and metadata
            if (mail.CustomHeaders is not null)
                foreach (var header in mail.CustomHeaders)
                    email.Headers.Add(header.Key, header.Value);

            if (!string.IsNullOrWhiteSpace(mail.Priority))
                email.Headers.Add("X-Priority", mail.Priority);
            if (mail.UnsubscribeLink != null)
                email.Headers.Add("List-Unsubscribe", $"<{mail.UnsubscribeLink}>");

            // Prepare email body and attachments
            BodyBuilder bodyBuilder = new() { TextBody = mail.TextBody, HtmlBody = mail.HtmlBody };
            mail.Attachments?.Where(attachment => attachment is not null)
                .ToList()
                .ForEach(attachment => bodyBuilder.Attachments.Add(attachment));

            email.Body = bodyBuilder.ToMessageBody();
            email.Prepare(EncodingConstraint.SevenBit);

            // Apply DKIM signature if configured
            if (hasValidDkimSettings())
            {
                applyDkimSignature(email);
            }

            // Configure and authenticate SMTP client
            await smtp.ConnectAsync(_mailSettings.Server, _mailSettings.Port, cancellationToken: cancellationToken);
            if (_mailSettings.AuthenticationRequired)
                await smtp.AuthenticateAsync(_mailSettings.UserName, _mailSettings.Password, cancellationToken);

            return (email, smtp);
        }
        catch
        {
            email.Dispose();
            smtp.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Checks if all required DKIM settings are provided.
    /// </summary>
    private bool hasValidDkimSettings() =>
        !string.IsNullOrWhiteSpace(_mailSettings.DkimPrivateKey)
        && !string.IsNullOrWhiteSpace(_mailSettings.DkimSelector)
        && !string.IsNullOrWhiteSpace(_mailSettings.DomainName);

    /// <summary>
    /// Applies DKIM signature to the email message.
    /// </summary>
    /// <param name="email">Email message to be signed.</param>
    private void applyDkimSignature(MimeMessage email)
    {
        DkimSigner signer = new(key: readPrivateKeyFromPemEncodedString(), _mailSettings.DomainName, _mailSettings.DkimSelector)
        {
            HeaderCanonicalizationAlgorithm = DkimCanonicalizationAlgorithm.Simple,
            BodyCanonicalizationAlgorithm = DkimCanonicalizationAlgorithm.Simple,
            AgentOrUserIdentifier = $"@{_mailSettings.DomainName}",
            QueryMethod = "dns/txt",
        };
        HeaderId[] headers = [HeaderId.From, HeaderId.Subject, HeaderId.To];
        signer.Sign(email, headers);
    }

    /// <summary>
    /// Reads and parses a PEM-encoded RSA private key.
    /// </summary>
    /// <returns>The parsed asymmetric key parameter.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the key format is invalid.</exception>
    private AsymmetricKeyParameter readPrivateKeyFromPemEncodedString()
    {
        string trimmedKey = (_mailSettings.DkimPrivateKey ?? string.Empty).Trim();
        string pemEncodedKey = trimmedKey.StartsWith("-----BEGIN", StringComparison.Ordinal)
            ? trimmedKey
            : "-----BEGIN RSA PRIVATE KEY-----\n" + trimmedKey + "\n-----END RSA PRIVATE KEY-----";

        using (StringReader stringReader = new(pemEncodedKey))
        {
            PemReader pemReader = new(stringReader);
            object? pemObject = pemReader.ReadObject();
            if (pemObject is AsymmetricCipherKeyPair keyPair)
                return keyPair.Private;
            else if (pemObject is AsymmetricKeyParameter keyParameter)
                return keyParameter;
            else
                throw new InvalidOperationException("Invalid key format");
        }
    }
}
