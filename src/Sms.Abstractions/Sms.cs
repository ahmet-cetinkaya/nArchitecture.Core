namespace NArchitecture.Core.Sms.Abstractions;

/// <summary>
/// Represents the details of an SMS message.
/// </summary>
/// <param name="PhoneNumber">The recipient's phone number in international format (e.g., +901234567890).</param>
/// <param name="Content">The text content of the SMS message.</param>
public readonly record struct Sms(string PhoneNumber, string Content)
{
    /// <summary>
    /// Gets or sets custom parameters to include in the SMS.
    /// </summary>
    public Dictionary<string, string>? CustomParameters { get; init; }

    /// <summary>
    /// Gets or sets the SMS priority (e.g., 1 for High, 3 for Normal, 5 for Low).
    /// </summary>
    public int? Priority { get; init; }
}
