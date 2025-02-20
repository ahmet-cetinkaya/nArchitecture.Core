namespace NArchitecture.Core.Mailing.Abstractions;

/// <summary>
/// Defines the contract for sending mail asynchronously.
/// </summary>
public interface IMailService
{
    /// <summary>
    /// Sends an email asynchronously.
    /// </summary>
    /// <param name="mail">The mail details.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendAsync(Mail mail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends multiple emails asynchronously.
    /// </summary>
    /// <param name="mailList">The list of emails to send.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendBulkAsync(IEnumerable<Mail> mailList, CancellationToken cancellationToken = default);
}
