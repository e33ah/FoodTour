namespace FoodTour.Test.Services;

public class SmtpEmailSenderTests {
    private const string TO_EMAIL = "recipient@test.com";
    private const string HTML_BODY = "<b>body</b>";
    private const string EMAIL_SUBJECT = "subject";

    private static EmailSettings CreateEmailSettings() => new() {
        Host = "smtp.test",
        Port = 25,
        FromEmail = "sender@test.com",
        FromName = "Test Sender"
    };

    [Fact]
    public async Task SendEmailAsync_ShouldThrowArgumentException_WhenToIsNull() {
        var emailSettings = CreateEmailSettings();
        var options = Options.Create(emailSettings);
        var sut = new SmtpEmailSender(options);

        var exception = await Should.ThrowAsync<ArgumentException>(async () =>
            await sut.SendEmailAsync(null!, EMAIL_SUBJECT, HTML_BODY)
        );

        exception.ParamName.ShouldBe("to");
        exception.Message.ShouldBe(new ArgumentException("Recipient email address is required.", "to").Message);
    }

    [Fact]
    public async Task SendEmailAsync_ShouldThrowArgumentException_WhenSubjectIsNull() {
        var emailSettings = CreateEmailSettings();
        var options = Options.Create(emailSettings);
        var sut = new SmtpEmailSender(options);

        var exception = await Should.ThrowAsync<ArgumentException>(async () =>
            await sut.SendEmailAsync(TO_EMAIL, null!, HTML_BODY)
            );
        exception.ParamName.ShouldBe("subject");
        exception.Message.ShouldBe(new ArgumentException("Email subject is required.", "subject").Message);
    }

    [Fact]
    public async Task SendEmailAsync_ShouldThrowArgumentException_WhenHtmlBodyIsNull() {
        var emailSettings = CreateEmailSettings();
        var options = Options.Create(emailSettings);
        var sut = new SmtpEmailSender(options);

        var exception = await Should.ThrowAsync<ArgumentException>(async () =>
            await sut.SendEmailAsync(TO_EMAIL, EMAIL_SUBJECT, null!)
            );
        exception.ParamName.ShouldBe("htmlBody");
        exception.Message.ShouldBe(new ArgumentException("Email body is required.", "htmlBody").Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendEmailAsync_ShouldThrowInvalidOperationException_WhenHostIsNotConfigured(string? host) {
        var emailSettings = CreateEmailSettings();
        emailSettings.Host = host!;
        var options = Options.Create(emailSettings);
        var sut = new SmtpEmailSender(options);

        var result = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await sut.SendEmailAsync(TO_EMAIL, EMAIL_SUBJECT, HTML_BODY)
            );

        result.Message.ShouldBe("EmailSettings.Host is not configured.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task SendEmailAsync_ShouldThrowInvalidOperationException_WhenPortIsNotConfigured(int port) {
        var emailSettings = CreateEmailSettings();
        emailSettings.Port = port;
        var options = Options.Create(emailSettings);
        var sut = new SmtpEmailSender(options);

        var result = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await sut.SendEmailAsync(TO_EMAIL, EMAIL_SUBJECT, HTML_BODY)
            );

        result.Message.ShouldBe("EmailSettings.Port is not configured.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendEmailAsync_ShouldThrowInvalidOperationException_WhenFromEmailIsNotConfigured(string? fromEmail) {
        var emailSettings = CreateEmailSettings();
        emailSettings.FromEmail = fromEmail!;
        var options = Options.Create(emailSettings);
        var sut = new SmtpEmailSender(options);

        var result = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await sut.SendEmailAsync(TO_EMAIL, EMAIL_SUBJECT, HTML_BODY)
            );

        result.Message.ShouldBe("EmailSettings.FromEmail is not configured.");
    }

}
