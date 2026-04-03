namespace FoodTour.Models;

public class Supplier {
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(100)]
    public string ContactName { get; set; } = null!;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string ContactEmail { get; set; } = null!;

    [MaxLength(500)]
    public string? OpeningTimes { get; set; }

    public bool IsActive { get; set; }

    [MaxLength(50)]
    public string? Tag1 { get; set; }

    [MaxLength(50)]
    public string? Tag2 { get; set; }

    [MaxLength(50)]
    public string? Tag3 { get; set; }

    [MaxLength(50)]
    public string? Tag4 { get; set; }

    [MaxLength(50)]
    public string ConfirmationCode { get; set; } = null!;

    public bool EmailNotificationsEnabled { get; set; }
}