namespace NArchitecture.Core.Sms.Abstractions;

/// <summary>
/// Defines the contract for sending SMS messages asynchronously.
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Sends an SMS message asynchronously.
    /// </summary>
    /// <param name="sms">The SMS details.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendAsync(Sms sms, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends multiple SMS messages asynchronously.
    /// </summary>
    /// <param name="smsList">The list of SMS messages to send.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendBulkAsync(IEnumerable<Sms> smsList, CancellationToken cancellationToken = default);
}
