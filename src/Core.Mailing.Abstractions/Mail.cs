using MimeKit;

namespace NArchitecture.Core.Mailing.Abstractions;

/// <summary>
/// Represents the details of an email message.
/// </summary>
public record Mail
{
    /// <summary>
    /// Gets or sets the subject of the email.
    /// </summary>
    public string Subject { get; set; }

    /// <summary>
    /// Gets or sets the plain text body of the email.
    /// </summary>
    public string TextBody { get; set; }

    /// <summary>
    /// Gets or sets the HTML body of the email.
    /// </summary>
    public string HtmlBody { get; set; }

    /// <summary>
    /// Gets or sets the collection of attachments for the email.
    /// </summary>
    public AttachmentCollection? Attachments { get; set; }

    /// <summary>
    /// Gets or sets the list of primary recipients.
    /// </summary>
    public List<MailboxAddress> ToList { get; set; }

    /// <summary>
    /// Gets or sets the list of CC recipients.
    /// </summary>
    public List<MailboxAddress>? CcList { get; set; }

    /// <summary>
    /// Gets or sets the list of BCC recipients.
    /// </summary>
    public List<MailboxAddress>? BccList { get; set; }

    /// <summary>
    /// Gets or sets the unsubscribe link for the email.
    /// </summary>
    public string? UnsubscribeLink { get; set; }

    /// <summary>
    /// Gets or sets the list of reply-to addresses.
    /// </summary>
    public List<MailboxAddress>? ReplyTo { get; set; }

    /// <summary>
    /// Gets or sets custom headers to include in the email.
    /// </summary>
    public Dictionary<string, string>? CustomHeaders { get; set; }

    /// <summary>
    /// Gets or sets the email priority (e.g., "1" for High, "3" for Normal, "5" for Low).
    /// </summary>
    public string? Priority { get; set; }

    public Mail()
    {
        Subject = string.Empty;
        TextBody = string.Empty;
        HtmlBody = string.Empty;
        ToList = [];
    }

    public Mail(string subject, string textBody, string htmlBody, List<MailboxAddress> toList)
        : this()
    {
        Subject = subject;
        TextBody = textBody;
        HtmlBody = htmlBody;
        ToList = toList;
    }
}
