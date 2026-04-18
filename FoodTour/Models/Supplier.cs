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
    [SemicolonSeparatedEmailAddresses]
    [MaxLength(256)]
    public string ContactEmail { get; set; } = null!;

    [MaxLength(500)]
    public string? OpeningTimes { get; set; }

    [MaxLength(500)]
    public string? OpeningTimesMonday { get; set; }

    [MaxLength(500)]
    public string? OpeningTimesTuesday { get; set; }

    [MaxLength(500)]
    public string? OpeningTimesWednesday { get; set; }

    [MaxLength(500)]
    public string? OpeningTimesThursday { get; set; }

    [MaxLength(500)]
    public string? OpeningTimesFriday { get; set; }

    [MaxLength(500)]
    public string? OpeningTimesSaturday { get; set; }

    [MaxLength(500)]
    public string? OpeningTimesSunday { get; set; }

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


    /// <summary>
    /// Returns an array of email addresses parsed from the ContactEmail property.
    /// </summary>
    public string[] GetContactEmailAddresses() {
        if (string.IsNullOrWhiteSpace(ContactEmail)) {
            return [];
        }

        return ContactEmail.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Gets the opening times for a specific day of the week.
    /// Falls back to the general OpeningTimes if day-specific time is not set.
    /// </summary>
    public string? GetOpeningTimesForDay(DayOfWeek dayOfWeek) =>
        dayOfWeek switch {
            DayOfWeek.Monday => OpeningTimesMonday ?? OpeningTimes,
            DayOfWeek.Tuesday => OpeningTimesTuesday ?? OpeningTimes,
            DayOfWeek.Wednesday => OpeningTimesWednesday ?? OpeningTimes,
            DayOfWeek.Thursday => OpeningTimesThursday ?? OpeningTimes,
            DayOfWeek.Friday => OpeningTimesFriday ?? OpeningTimes,
            DayOfWeek.Saturday => OpeningTimesSaturday ?? OpeningTimes,
            DayOfWeek.Sunday => OpeningTimesSunday ?? OpeningTimes,
            _ => OpeningTimes
        };
}
