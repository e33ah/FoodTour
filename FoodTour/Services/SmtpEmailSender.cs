namespace FoodTour.Services;

public class SmtpEmailSender(IOptions<EmailSettings> options) : IEmailSender {
    private readonly EmailSettings settings = options.Value;

    public async Task SendEmailAsync(string to, string subject, string htmlBody) {
        ValidateEmailParameters(to, subject, htmlBody);

        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(settings.FromName, settings.FromEmail));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;

        msg.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        try {
            var socketOptions = settings.UseSsl
                ? MailKit.Security.SecureSocketOptions.SslOnConnect
                : MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable;

            await client.ConnectAsync(settings.Host, settings.Port, socketOptions);

            if (!string.IsNullOrWhiteSpace(settings.Username)) {
                if (string.IsNullOrWhiteSpace(settings.Password)) {
                    throw new InvalidOperationException("EmailSettings.Password is required when Username is provided.");
                }
                await client.AuthenticateAsync(settings.Username, settings.Password);
            }

            await client.SendAsync(msg);
        } catch (Exception ex) {
            throw new InvalidOperationException("Failed to send email.", ex);
        } finally {
            if (client.IsConnected) {
                try {
                    await client.DisconnectAsync(true);
                } catch {
                    // Ignore exceptions during disconnect to avoid masking the original exception
                }
            }
        }
    }

    private void ValidateEmailParameters(string to, string subject, string htmlBody) {
        if (string.IsNullOrWhiteSpace(to)) {
            throw new ArgumentException("Recipient email address is required.", nameof(to));
        }
        if (string.IsNullOrWhiteSpace(subject)) {
            throw new ArgumentException("Email subject is required.", nameof(subject));
        }
        if (string.IsNullOrWhiteSpace(htmlBody)) {
            throw new ArgumentException("Email body is required.", nameof(htmlBody));
        }
        if (string.IsNullOrWhiteSpace(settings.Host)) {
            throw new InvalidOperationException("EmailSettings.Host is not configured.");
        }
        if (settings.Port <= 0) {
            throw new InvalidOperationException("EmailSettings.Port is not configured.");
        }
        if (string.IsNullOrWhiteSpace(settings.FromEmail)) {
            throw new InvalidOperationException("EmailSettings.FromEmail is not configured.");
        }
    }
}
