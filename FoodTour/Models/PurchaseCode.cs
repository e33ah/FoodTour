namespace FoodTour.Models;

public class PurchaseCode {
    public int Id { get; set; }
    [Required]
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
