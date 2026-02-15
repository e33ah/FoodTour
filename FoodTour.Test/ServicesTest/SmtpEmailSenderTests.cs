namespace FoodTour.Test.ServicesTest;

public class SmtpEmailSenderTests {
    private const string TO_EMAIL = "recipient@test.com";
    private const string HTML_BODY = "<b>body</b>";
    private const string EMAIL_SUBJECT = "subject";

    [Fact]
    public async Task SendEmailAsync_ShouldThrowArgumentException_WhenToIsNull() {
        var emailSettings = TestHelper.testEmailSettings;
        var options = Options.Create(emailSettings);
        var sut = new SmtpEmailSender(options);

        var exception = await Should.ThrowAsync<ArgumentException>(async () =>
            await sut.SendEmailAsync(null!, EMAIL_SUBJECT, HTML_BODY)
        );

        exception.ParamName.ShouldBe("to");
    }

    [Fact]
    public async Task SendEmailAsync_ShouldThrowArgumentException_WhenSubjectIsNull() {
        var emailSettings = TestHelper.testEmailSettings;
        var options = Options.Create(emailSettings);
        var sut = new SmtpEmailSender(options);

        var exception = await Should.ThrowAsync<ArgumentException>(async () =>
            await sut.SendEmailAsync(TO_EMAIL, null!, HTML_BODY)
            );
        exception.ParamName.ShouldBe("subject");
    }

    [Fact]
    public async Task SendEmailAsync_ShouldThrowArgumentException_WhenHtmlBodyIsNull() {
        var emailSettings = TestHelper.testEmailSettings;
        var options = Options.Create(emailSettings);
        var sut = new SmtpEmailSender(options);

        var exception = await Should.ThrowAsync<ArgumentException>(async () =>
            await sut.SendEmailAsync(TO_EMAIL, EMAIL_SUBJECT, null!)
            );
        exception.ParamName.ShouldBe("htmlBody");
    }

    [Fact]
    public async Task SendEmailAsync_ShouldThrowInvalidOperationException_WhenHostIsNotConfigured() {
        var emailSettings = TestHelper.testEmailSettings;
        emailSettings.Host = null!;
        var options = Options.Create(emailSettings);
        var sut = new SmtpEmailSender(options);

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await sut.SendEmailAsync(TO_EMAIL, EMAIL_SUBJECT, HTML_BODY)
            );
    }

    [Fact]
    public async Task SendEmailAsync_ShouldThrowInvalidOperationException_WhenPortIsNotConfigured() {
        var emailSettings = TestHelper.testEmailSettings;
        emailSettings.Port = 0;
        var options = Options.Create(emailSettings);
        var sut = new SmtpEmailSender(options);

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await sut.SendEmailAsync(TO_EMAIL, EMAIL_SUBJECT, HTML_BODY)
            );
    }

    [Fact]
    public async Task SendEmailAsync_ShouldThrowInvalidOperationException_WhenFromEmailIsNotConfigured() {
        var emailSettings = TestHelper.testEmailSettings;
        emailSettings.FromEmail = null!;
        var options = Options.Create(emailSettings);
        var sut = new SmtpEmailSender(options);

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await sut.SendEmailAsync(TO_EMAIL, EMAIL_SUBJECT, HTML_BODY)
            );
    }

    [Fact]
    public async Task SendEmailAsync_ShouldThrowInvalidOperationException_WhenSmtpConnectionFails() {
        var emailSettings = TestHelper.testEmailSettings;
        emailSettings.Host = "invalid.smtp.host";
        var options = Options.Create(emailSettings);
        var sut = new SmtpEmailSender(options);

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await sut.SendEmailAsync(TO_EMAIL, EMAIL_SUBJECT, HTML_BODY)
            );
    }

    [Fact]
    public async Task SendEmailAsync_ShouldThrowInvalidOperationException_WhenPasswordIsNotConfigured() {
        var emailSettings = TestHelper.testEmailSettings;
        emailSettings.Username = "valid_user";
        var options = Options.Create(emailSettings);
        var sut = new SmtpEmailSender(options);

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await sut.SendEmailAsync(TO_EMAIL, EMAIL_SUBJECT, HTML_BODY)
            );
    }

    [Fact]
    public async Task SendEmailAsync_ShouldThrowInvalidOperationException_WhenAuthenticationFails() {
        var emailSettings = TestHelper.testEmailSettings;
        emailSettings.Username = "invalid_user";
        emailSettings.Password = "invalid_password";
        var options = Options.Create(emailSettings);
        var sut = new SmtpEmailSender(options);

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await sut.SendEmailAsync(TO_EMAIL, EMAIL_SUBJECT, HTML_BODY)
            );
    }
}