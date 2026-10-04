namespace FoodTour.ITest.Services;

internal sealed class LocalSmtpServer : IAsyncDisposable {
    private const int TIMEOUT_SECONDS = 10;
    private readonly TcpListener listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource cancellation = new(TimeSpan.FromSeconds(TIMEOUT_SECONDS));
    private readonly Task serverTask;

    public int Port { get; }
    public bool AuthenticationAttempted { get; private set; }
    public string Message { get; private set; } = string.Empty;

    public LocalSmtpServer(bool rejectConnection = false) {
        listener.Start();
        Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        serverTask = ServeAsync(rejectConnection);
    }

    private async Task ServeAsync(bool rejectConnection) {
        using var client = await listener.AcceptTcpClientAsync(cancellation.Token);
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        await using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) {
            AutoFlush = true,
            NewLine = "\r\n"
        };
        await writer.WriteLineAsync(rejectConnection ? "421 Service unavailable" : "220 localhost SMTP");
        if (rejectConnection) {
            return;
        }

        while (await reader.ReadLineAsync(cancellation.Token) is string command) {
            if (command.StartsWith("EHLO ", StringComparison.OrdinalIgnoreCase)) {
                await writer.WriteLineAsync("250-localhost");
                await writer.WriteLineAsync("250 AUTH PLAIN");
            } else if (command.StartsWith("AUTH ", StringComparison.OrdinalIgnoreCase)) {
                AuthenticationAttempted = true;
                await writer.WriteLineAsync("535 Authentication rejected");
            } else if (command == "DATA") {
                await writer.WriteLineAsync("354 Send message");
                var message = new StringBuilder();
                while (await reader.ReadLineAsync(cancellation.Token) is string line && line != ".") {
                    message.AppendLine(line);
                }
                Message = message.ToString();
                await writer.WriteLineAsync("250 Accepted");
            } else if (command == "QUIT") {
                await writer.WriteLineAsync("221 Goodbye");
                return;
            } else if (command.StartsWith("MAIL FROM:", StringComparison.OrdinalIgnoreCase) ||
                       command.StartsWith("RCPT TO:", StringComparison.OrdinalIgnoreCase) || command == "RSET") {
                await writer.WriteLineAsync("250 OK");
            } else {
                throw new InvalidOperationException($"Unexpected SMTP command: {command}");
            }
        }
    }

    public async ValueTask DisposeAsync() {
        await cancellation.CancelAsync();
        listener.Stop();
        try {
            await serverTask;
        } catch (OperationCanceledException) when (cancellation.IsCancellationRequested) {
            // Cancellation releases a pending accept or read when a test exits early.
        } finally {
            cancellation.Dispose();
        }
    }
}
