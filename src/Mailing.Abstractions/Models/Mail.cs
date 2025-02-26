using MimeKit;

namespace NArchitecture.Core.Mailing.Abstractions.Models;

/// <summary>
/// Represents an immutable email message structure with its content and recipients.
/// </summary>
/// <param name="Subject">The subject line of the email (e.g., "Welcome to Our Service")</param>
/// <param name="TextBody">The plain text version of the email body for clients that don't support HTML</param>
/// <param name="HtmlBody">The HTML version of the email body for rich formatting</param>
/// <param name="ToList">List of primary recipients' email addresses</param>
public readonly record struct Mail(string Subject, string TextBody, string HtmlBody, List<MailboxAddress> ToList)
{
    /// <summary>
    /// Gets the collection of file attachments to be included with the email.
    /// Supports various file types and formats.
    /// </summary>
    public AttachmentCollection? Attachments { get; init; }

    /// <summary>
    /// Gets the list of CC (Carbon Copy) recipients who should receive a copy of the email.
    /// Recipients in this list can see other CC recipients.
    /// </summary>
    public List<MailboxAddress>? CcList { get; init; }

    /// <summary>
    /// Gets the list of BCC (Blind Carbon Copy) recipients.
    /// These recipients are hidden from other recipients.
    /// </summary>
    public List<MailboxAddress>? BccList { get; init; }

    /// <summary>
    /// Gets the URL for unsubscribing from the mailing list.
    /// Will be included as a List-Unsubscribe header in the email.
    /// </summary>
    public string? UnsubscribeLink { get; init; }

    /// <summary>
    /// Gets the list of email addresses that should receive replies to this email.
    /// Overrides the default reply-to behavior.
    /// </summary>
    public List<MailboxAddress>? ReplyTo { get; init; }

    /// <summary>
    /// Gets custom email headers for specialized email handling or tracking.
    /// </summary>
    public Dictionary<string, string>? CustomHeaders { get; init; }

    /// <summary>
    /// Gets the priority level of the email.
    /// </summary>
    public int? Priority { get; init; }
}
