namespace FoodTour.Models;

public class BookingConfirmationVM {
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public Guid QrToken { get; set; } = Guid.Empty;
    public string? EmailWarning { get; set; }
}
