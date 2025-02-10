using MailKit;
using MailKit.Net.Smtp;
using MimeKit;
using Moq;
using NArchitecture.Core.Mailing.Abstractions;
using NArchitecture.Core.Mailing.MailKit;
using Shouldly;
using Xunit;

namespace Core.Mailing.Tests;

public class MailKitMailServiceTests
{
    private readonly Mock<ISmtpClient> _smtpClientMock;
    private readonly Mock<ISmtpClientFactory> _smtpClientFactoryMock;
    private readonly MailSettings _mailSettings;
    private readonly MailKitMailService _sut; // System Under Test

    public MailKitMailServiceTests()
    {
        _smtpClientMock = new Mock<ISmtpClient>();
        _smtpClientFactoryMock = new Mock<ISmtpClientFactory>();
        _smtpClientFactoryMock.Setup(x => x.Create()).Returns(_smtpClientMock.Object);

        _mailSettings = new MailSettings
        {
            Server = "smtp.example.com",
            Port = 587,
            UserName = "test@example.com",
            Password = "password123",
            SenderEmail = "sender@example.com",
            SenderFullName = "Test Sender",
            AuthenticationRequired = true,
        };

        _sut = new MailKitMailService(_mailSettings, _smtpClientFactoryMock.Object);
    }

    [Fact(DisplayName = "Should successfully send email with valid parameters")]
    public async Task SendEmailAsync_WithValidParameters_ShouldSendEmail()
    {
        // Arrange
        var mail = new Mail
        {
            Subject = "Test Subject",
            TextBody = "Test Body",
            HtmlBody = "<p>Test Body</p>",
            ToList = [new("Test Recipient", "recipient@example.com")],
        };

        _ = _smtpClientMock
            .Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(""));

        _ = _smtpClientMock
            .Setup(x => x.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(""));

        _ = _smtpClientMock
            .Setup(x => x.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), null))
            .Returns(Task.FromResult(""));

        // Act
        await _sut.SendEmailAsync(mail);

        // Assert
        _smtpClientMock.Verify(
            x => x.SendAsync(It.Is<MimeMessage>(m => m.Subject == mail.Subject), It.IsAny<CancellationToken>(), null),
            Times.Once
        );
    }

    [Fact(DisplayName = "Should not send email when recipient list is empty")]
    public async Task SendEmailAsync_WithEmptyRecipientList_ShouldNotSendEmail()
    {
        // Arrange
        var mail = new Mail
        {
            Subject = "Test Subject",
            TextBody = "Test Body",
            ToList = [],
        };

        // Act
        await _sut.SendEmailAsync(mail);

        // Assert
        _smtpClientMock.Verify(x => x.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), null), Times.Never);
    }

    [Fact(DisplayName = "Should handle SMTP connection failure")]
    public async Task SendEmailAsync_WhenSmtpConnectionFails_ShouldThrowException()
    {
        // Arrange
        var mail = new Mail
        {
            Subject = "Test Subject",
            TextBody = "Test Body",
            ToList = [new("Test", "test@example.com")],
        };

        var expectedException = new ServiceNotConnectedException("Connection failed");

        _smtpClientMock
            .Setup(x =>
                x.ConnectAsync(
                    _mailSettings.Server,
                    _mailSettings.Port,
                    It.IsAny<MailKit.Security.SecureSocketOptions>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(expectedException);

        _smtpClientMock
            .Setup(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await Should.ThrowAsync<ServiceNotConnectedException>(async () =>
        {
            await _sut.SendEmailAsync(mail);
        });
    }

    [Fact(DisplayName = "Should respect cancellation token")]
    public async Task SendEmailAsync_WhenCancellationRequested_ShouldCancelOperation()
    {
        // Arrange
        var mail = new Mail
        {
            Subject = "Test Subject",
            TextBody = "Test Body",
            ToList = [new("Test", "test@example.com")],
        };

        var cts = new CancellationTokenSource();

        _smtpClientMock
            .Setup(x =>
                x.ConnectAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<MailKit.Security.SecureSocketOptions>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(
                (string host, int port, MailKit.Security.SecureSocketOptions options, CancellationToken token) =>
                    Task.FromCanceled(token)
            );

        _smtpClientMock
            .Setup(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        cts.Cancel(); // Cancel before execution

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await _sut.SendEmailAsync(mail, cts.Token);
        });
    }

    [Theory(DisplayName = "Should handle various recipient combinations")]
    [InlineData(true, false, false)] // Only CC
    [InlineData(false, true, false)] // Only BCC
    [InlineData(true, true, false)] // CC and BCC
    [InlineData(false, false, true)] // Only ReplyTo
    [InlineData(true, true, true)] // All recipient types
    public async Task SendEmailAsync_WithVariousRecipients_ShouldHandleCorrectly(
        bool includeCc,
        bool includeBcc,
        bool includeReplyTo
    )
    {
        // Arrange
        var mail = new Mail
        {
            Subject = "Test Subject",
            TextBody = "Test Body",
            ToList = [new("Primary", "primary@example.com")],
        };

        if (includeCc)
            mail.CcList = [new("CC", "cc@example.com")];
        if (includeBcc)
            mail.BccList = [new("BCC", "bcc@example.com")];
        if (includeReplyTo)
            mail.ReplyTo = [new("Reply", "reply@example.com")];

        _ = _smtpClientMock
            .Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SendEmailAsync(mail);

        // Assert
        _smtpClientMock.Verify(
            x =>
                x.SendAsync(
                    It.Is<MimeMessage>(m =>
                        (!includeCc || m.Cc.Count > 0)
                        && (!includeBcc || m.Bcc.Count > 0)
                        && (!includeReplyTo || m.ReplyTo.Count > 0)
                    ),
                    It.IsAny<CancellationToken>(),
                    null
                ),
            Times.Once
        );
    }
}
