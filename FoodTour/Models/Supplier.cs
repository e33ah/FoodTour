namespace FoodTour.Models;

public class Supplier {
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public string? Description { get; set; }
    public string ContactName { get; set; } = null!;
    [EmailAddress]
    public string ContactEmail { get; set; } = null!;
    public bool IsActive { get; set; }
    public string? Tag1 { get; set; }
    public string? Tag2 { get; set; }
    public string? Tag3 { get; set; }
    public string? Tag4 { get; set; }
}