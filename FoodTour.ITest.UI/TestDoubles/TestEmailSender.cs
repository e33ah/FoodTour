namespace FoodTour.ITest.UI.TestDoubles;

public sealed class TestEmailSender : IEmailSender {
    private readonly List<SentEmail> sentEmails = [];
    private readonly Lock syncRoot = new();

    public IReadOnlyList<SentEmail> SentEmails {
        get {
            lock (syncRoot) {
                return [.. sentEmails];
            }
        }
    }

    public Task SendEmailAsync(string to, string subject, string htmlBody) {
        lock (syncRoot) {
            sentEmails.Add(new SentEmail(to, subject, htmlBody));
        }

        return Task.CompletedTask;
    }

    public void Clear() {
        lock (syncRoot) {
            sentEmails.Clear();
        }
    }
}

public sealed record SentEmail(string To, string Subject, string HtmlBody);
