using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Cryptography;
using NArchitecture.Core.Mailing.Abstractions;
using NArchitecture.Core.Mailing.Abstractions.Models;
using NArchitecture.Core.Mailing.MailKit.Models;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;

namespace NArchitecture.Core.Mailing.MailKit;

/// <summary>
/// Implements email sending functionality using MailKit library.
/// </summary>
public class MailKitMailService(MailConfigration configuration, ISmtpClientFactory? smtpClientFactory = null) : IMailService
{
    private readonly ISmtpClientFactory _smtpClientFactory = smtpClientFactory ?? new DefaultSmtpClientFactory();

    /// <summary>
    /// Sends an email message asynchronously.
    /// </summary>
    /// <param name="mail">The mail message to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public virtual async Task SendAsync(Mail mail, CancellationToken cancellationToken = default)
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
    /// Sends multiple email messages asynchronously in bulk.
    /// </summary>
    /// <param name="mailList">The list of mail messages to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public virtual async Task SendBulkAsync(IEnumerable<Mail> mailList, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mailList);

        ISmtpClient? smtp = null;
        try
        {
            smtp = _smtpClientFactory.Create();
            await smtp.ConnectAsync(configuration.Server, configuration.Port, cancellationToken: cancellationToken);
            if (configuration.AuthenticationRequired)
                await smtp.AuthenticateAsync(configuration.UserName, configuration.Password, cancellationToken);

            foreach (Mail mail in mailList)
            {
                if (mail.ToList is null || mail.ToList.Count == 0)
                    continue;

                using MimeMessage email = PrepareMimeMessage(mail);
                _ = await smtp.SendAsync(email, cancellationToken);
            }
        }
        finally
        {
            if (smtp is not null)
            {
                await smtp.DisconnectAsync(true, cancellationToken);
                smtp.Dispose();
            }
        }
    }

    /// <summary>
    /// Prepares a MIME message from the given mail data.
    /// </summary>
    /// <param name="mail">Mail data to convert to MIME message.</param>
    protected virtual MimeMessage PrepareMimeMessage(Mail mail)
    {
        MimeMessage email = new();

        email.From.Add(new MailboxAddress(configuration.SenderFullName, configuration.SenderEmail));
        email.To.AddRange(mail.ToList);

        if (mail.CcList?.Count > 0)
            email.Cc.AddRange(mail.CcList);
        if (mail.BccList?.Count > 0)
            email.Bcc.AddRange(mail.BccList);
        if (mail.ReplyTo?.Count > 0)
            email.ReplyTo.AddRange(mail.ReplyTo);

        email.Subject = mail.Subject;

        if (mail.CustomHeaders is not null)
            foreach (KeyValuePair<string, string> header in mail.CustomHeaders)
                email.Headers.Add(header.Key, header.Value);

        if (mail.Priority.HasValue)
            email.Headers.Add("X-Priority", mail.Priority.Value.ToString());
        if (mail.UnsubscribeLink != null)
            email.Headers.Add("List-Unsubscribe", $"<{mail.UnsubscribeLink}>");

        BodyBuilder bodyBuilder = new() { TextBody = mail.TextBody, HtmlBody = mail.HtmlBody };
        mail.Attachments?.Where(attachment => attachment is not null).ToList().ForEach(bodyBuilder.Attachments.Add);

        email.Body = bodyBuilder.ToMessageBody();
        email.Prepare(EncodingConstraint.SevenBit);

        if (hasValidDkimSettings())
        {
            applyDkimSignature(email);
        }

        return email;
    }

    private async Task<(MimeMessage email, ISmtpClient smtp)> emailPrepareAsync(Mail mail, CancellationToken cancellationToken)
    {
        MimeMessage email = PrepareMimeMessage(mail);
        ISmtpClient smtp = _smtpClientFactory.Create();

        try
        {
            await smtp.ConnectAsync(configuration.Server, configuration.Port, cancellationToken: cancellationToken);
            if (configuration.AuthenticationRequired)
                await smtp.AuthenticateAsync(configuration.UserName, configuration.Password, cancellationToken);

            return (email, smtp);
        }
        catch
        {
            email.Dispose();
            smtp.Dispose();
            throw;
        }
    }

    private bool hasValidDkimSettings()
    {
        return !string.IsNullOrWhiteSpace(configuration.DkimPrivateKey)
            && !string.IsNullOrWhiteSpace(configuration.DkimSelector)
            && !string.IsNullOrWhiteSpace(configuration.DomainName);
    }

    private void applyDkimSignature(MimeMessage email)
    {
        DkimSigner signer = new(key: readPrivateKeyFromPemEncodedString(), configuration.DomainName, configuration.DkimSelector)
        {
            HeaderCanonicalizationAlgorithm = DkimCanonicalizationAlgorithm.Simple,
            BodyCanonicalizationAlgorithm = DkimCanonicalizationAlgorithm.Simple,
            AgentOrUserIdentifier = $"@{configuration.DomainName}",
            QueryMethod = "dns/txt",
        };
        HeaderId[] headers = [HeaderId.From, HeaderId.Subject, HeaderId.To];
        signer.Sign(email, headers);
    }

    private AsymmetricKeyParameter readPrivateKeyFromPemEncodedString()
    {
        string trimmedKey = (configuration.DkimPrivateKey ?? string.Empty).Trim();
        string pemEncodedKey = trimmedKey.StartsWith("-----BEGIN", StringComparison.Ordinal)
            ? trimmedKey
            : "-----BEGIN RSA PRIVATE KEY-----\n" + trimmedKey + "\n-----END RSA PRIVATE KEY-----";

        using StringReader stringReader = new(pemEncodedKey);
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
