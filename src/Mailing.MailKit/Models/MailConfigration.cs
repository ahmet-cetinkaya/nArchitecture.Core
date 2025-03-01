namespace NArchitecture.Core.Mailing.MailKit.Models;

/// <summary>
/// Contains configuration settings for sending mail using SMTP server.
/// </summary>
/// <param name="Server">SMTP server address (e.g., "smtp.gmail.com")</param>
/// <param name="Port">SMTP server port number (e.g., 587 for TLS, 465 for SSL)</param>
/// <param name="SenderFullName">Display name for the email sender (e.g., "John Doe")</param>
/// <param name="SenderEmail">Email address of the sender (e.g., "john@example.com")</param>
/// <param name="UserName">Username for SMTP authentication (typically the email address)</param>
/// <param name="Password">Password or app-specific password for SMTP authentication</param>
public readonly record struct MailConfigration(
    string Server,
    int Port,
    string SenderFullName,
    string SenderEmail,
    string UserName,
    string Password
)
{
    /// <summary>
    /// Gets or sets whether SMTP authentication should be performed.
    /// When true, the service will attempt to authenticate using the provided credentials.
    /// </summary>
    /// <value>Default is true as most modern SMTP servers require authentication.</value>
    public bool AuthenticationRequired { get; init; } = true;

    /// <summary>
    /// Gets or sets the private key used for DKIM (DomainKeys Identified Mail) signing.
    /// The key should be in PEM format and can be provided with or without header/footer.
    /// </summary>
    /// <value>The RSA private key in PEM format.</value>
    public string? DkimPrivateKey { get; init; }

    /// <summary>
    /// Gets or sets the selector used for DKIM signing.
    /// This value should match the selector configured in your domain's DNS records.
    /// </summary>
    /// <value>The DKIM selector name (e.g., "default", "mail", etc.)</value>
    public string? DkimSelector { get; init; }

    /// <summary>
    /// Gets or sets the domain name used for DKIM signing.
    /// This should be the domain from which the emails are being sent.
    /// </summary>
    /// <value>The domain name (e.g., "example.com")</value>
    public string? DomainName { get; init; }
}
