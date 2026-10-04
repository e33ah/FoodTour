namespace FoodTour.ITest.Services;

public class SmtpEmailSenderTests {
    private const string TO_EMAIL = "recipient@test.com";
    private const string EMAIL_SUBJECT = "Integration test";
    private const string HTML_BODY = "<b>Integration test body</b>";

    [Fact]
    public async Task SendEmailAsync_ThrowsWrappedException_WhenServerRejectsConnection() {
        await using var server = new LocalSmtpServer(rejectConnection: true);
        var settings = CreateSettings(server.Port);
        var sut = new SmtpEmailSender(Options.Create(settings));

        var result = await Should.ThrowAsync<InvalidOperationException>(() =>
            sut.SendEmailAsync(TO_EMAIL, EMAIL_SUBJECT, HTML_BODY));

        result.Message.ShouldBe("Failed to send email.");
        result.InnerException.ShouldBeOfType<MailKit.Net.Smtp.SmtpCommandException>();
    }

    [Fact]
    public async Task SendEmailAsync_ThrowsPasswordError_WhenUsernameIsProvidedWithoutPassword() {
        await using var server = new LocalSmtpServer();
        var settings = CreateSettings(server.Port);
        settings.Username = "test-user";
        settings.Password = string.Empty;
        var sut = new SmtpEmailSender(Options.Create(settings));

        var result = await Should.ThrowAsync<InvalidOperationException>(() =>
            sut.SendEmailAsync(TO_EMAIL, EMAIL_SUBJECT, HTML_BODY));

        result.Message.ShouldBe("Failed to send email.");
        var passwordError = result.InnerException.ShouldBeOfType<InvalidOperationException>();
        passwordError.Message.ShouldBe("EmailSettings.Password is required when Username is provided.");
        server.AuthenticationAttempted.ShouldBeFalse();
    }

    [Fact]
    public async Task SendEmailAsync_ThrowsAuthenticationError_WhenServerRejectsCredentials() {
        await using var server = new LocalSmtpServer();
        var settings = CreateSettings(server.Port);
        settings.Username = "test-user";
        settings.Password = "test-password";
        var sut = new SmtpEmailSender(Options.Create(settings));

        var result = await Should.ThrowAsync<InvalidOperationException>(() =>
            sut.SendEmailAsync(TO_EMAIL, EMAIL_SUBJECT, HTML_BODY));

        result.Message.ShouldBe("Failed to send email.");
        result.InnerException.ShouldBeOfType<MailKit.Security.AuthenticationException>();
        server.AuthenticationAttempted.ShouldBeTrue();
    }

    [Fact]
    public async Task SendEmailAsync_SendsMessage_WhenSenderDisplayNameIsMissing() {
        await using var server = new LocalSmtpServer();
        var settings = CreateSettings(server.Port);
        settings.FromName = null!;
        var sut = new SmtpEmailSender(Options.Create(settings));

        await sut.SendEmailAsync(TO_EMAIL, EMAIL_SUBJECT, HTML_BODY);

        server.Message.ShouldContain(HTML_BODY);
        server.Message.ShouldContain(EMAIL_SUBJECT);
        server.Message.ShouldContain(settings.FromEmail);
        server.Message.ShouldContain(TO_EMAIL);
    }

    private static EmailSettings CreateSettings(int port) => new() {
        Host = IPAddress.Loopback.ToString(),
        Port = port,
        UseSsl = false,
        FromEmail = "sender@test.com",
        FromName = "Test Sender"
    };
}
