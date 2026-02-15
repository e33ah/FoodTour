namespace FoodTour.Services;

public class EmailSettings {
    public required string Host { get; set; }
    public int Port { get; set; }
    public bool UseSsl { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public required string FromEmail { get; set; }
    public required string FromName { get; set; }
}
