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
    Task SendEmailAsync(Mail mail, CancellationToken cancellationToken = default);
}
