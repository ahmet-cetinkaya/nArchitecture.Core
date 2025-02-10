namespace NArchitecture.Core.Mailing.Abstractions;

/// <summary>
/// Contains configuration settings for sending mail.
/// </summary>
public record MailSettings
{
    /// <summary>
    /// Gets or sets the mail server address.
    /// </summary>
    public string Server { get; set; }

    /// <summary>
    /// Gets or sets the mail server port.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the display name of the email sender.
    /// </summary>
    public string SenderFullName { get; set; }

    /// <summary>
    /// Gets or sets the sender's email address.
    /// </summary>
    public string SenderEmail { get; set; }

    /// <summary>
    /// Gets or sets the username for mail server authentication.
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// Gets or sets the password for mail server authentication.
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether authentication is required.
    /// </summary>
    public bool AuthenticationRequired { get; set; }

    /// <summary>
    /// Gets or sets the DKIM private key for signing messages.
    /// </summary>
    public string? DkimPrivateKey { get; set; }

    /// <summary>
    /// Gets or sets the DKIM selector.
    /// </summary>
    public string? DkimSelector { get; set; }

    /// <summary>
    /// Gets or sets the domain name for DKIM signing.
    /// </summary>
    public string? DomainName { get; set; }

    public MailSettings()
    {
        Server = string.Empty;
        Port = 0;
        SenderFullName = string.Empty;
        SenderEmail = string.Empty;
        UserName = string.Empty;
        Password = string.Empty;
    }

    public MailSettings(
        string server,
        int port,
        string senderFullName,
        string senderEmail,
        string userName,
        string password,
        bool authenticationRequired
    )
        : this()
    {
        Server = server;
        Port = port;
        SenderFullName = senderFullName;
        SenderEmail = senderEmail;
        UserName = userName;
        Password = password;
        AuthenticationRequired = authenticationRequired;
    }
}
