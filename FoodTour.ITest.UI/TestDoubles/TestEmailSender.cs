namespace FoodTour.ITest.UI.TestDoubles;

public sealed class TestEmailSender : IEmailSender {
    private readonly List<SentEmail> sentEmails = [];
    private readonly Lock syncRoot = new();

    public bool ThrowOnSend { get; set; }

    public IReadOnlyList<SentEmail> SentEmails {
        get {
            lock (syncRoot) {
                return [.. sentEmails];
            }
        }
    }

    public Task SendEmailAsync(string to, string subject, string htmlBody) {
        lock (syncRoot) {
            if (ThrowOnSend) {
                throw new InvalidOperationException("The test email sender is configured to fail.");
            }

            sentEmails.Add(new SentEmail(to, subject, htmlBody));
        }

        return Task.CompletedTask;
    }

    public void Clear() {
        lock (syncRoot) {
            sentEmails.Clear();
            ThrowOnSend = false;
        }
    }
}

public sealed record SentEmail(string To, string Subject, string HtmlBody);
