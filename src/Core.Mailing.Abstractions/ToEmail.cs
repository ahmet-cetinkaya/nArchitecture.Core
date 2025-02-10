namespace NArchitecture.Core.Mailing.Abstractions;

/// <summary>
/// Represents an email recipient with an address and full name.
/// </summary>
public record ToEmail
{
    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// Gets or sets the full name of the recipient.
    /// </summary>
    public string FullName { get; set; }

    public ToEmail()
    {
        Email = string.Empty;
        FullName = string.Empty;
    }

    public ToEmail(string email, string fullName)
        : this()
    {
        Email = email;
        FullName = fullName;
    }
}
