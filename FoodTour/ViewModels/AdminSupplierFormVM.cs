namespace FoodTour.Models;

public class AdminSupplierFormVM {
    public int Id { get; set; }

    [Display(Name = "Business Name")]
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Business Address")]
    [MaxLength(500)]
    public string? Address { get; set; }

    [Display(Name = "Business Description")]
    [MaxLength(1000)]
    public string? Description { get; set; }

    [Display(Name = "Contact name")]
    [Required]
    [MaxLength(100)]
    public string ContactName { get; set; } = string.Empty;

    [Display(Name = "Contact email")]
    [Required]
    [MaxLength(256)]
    [SemicolonSeparatedEmailAddresses]
    public string ContactEmail { get; set; } = string.Empty;

    [Display(Name = "Opening times (general)")]
    [MaxLength(500)]
    public string? OpeningTimes { get; set; }

    [Display(Name = "Opening times (Monday)")]
    [MaxLength(500)]
    public string? OpeningTimesMonday { get; set; }

    [Display(Name = "Opening times (Tuesday)")]
    [MaxLength(500)]
    public string? OpeningTimesTuesday { get; set; }

    [Display(Name = "Opening times (Wednesday)")]
    [MaxLength(500)]
    public string? OpeningTimesWednesday { get; set; }

    [Display(Name = "Opening times (Thursday)")]
    [MaxLength(500)]
    public string? OpeningTimesThursday { get; set; }

    [Display(Name = "Opening times (Friday)")]
    [MaxLength(500)]
    public string? OpeningTimesFriday { get; set; }

    [Display(Name = "Opening times (Saturday)")]
    [MaxLength(500)]
    public string? OpeningTimesSaturday { get; set; }

    [Display(Name = "Opening times (Sunday)")]
    [MaxLength(500)]
    public string? OpeningTimesSunday { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public bool OriginalIsActive { get; set; }

    [Display(Name = "Tag 1")]
    [MaxLength(50)]
    public string? Tag1 { get; set; }

    [Display(Name = "Tag 2")]
    [MaxLength(50)]
    public string? Tag2 { get; set; }

    [Display(Name = "Tag 3")]
    [MaxLength(50)]
    public string? Tag3 { get; set; }

    [Display(Name = "Tag 4")]
    [MaxLength(50)]
    public string? Tag4 { get; set; }

    [Display(Name = "Confirmation code")]
    [Required]
    [MaxLength(50)]
    public string ConfirmationCode { get; set; } = "TOUR";

    [Display(Name = "Email notifications enabled")]
    public bool EmailNotificationsEnabled { get; set; }

    public static AdminSupplierFormVM FromSupplier(Supplier supplier) {
        return new AdminSupplierFormVM {
            Id = supplier.Id,
            Name = supplier.Name,
            Address = supplier.Address,
            Description = supplier.Description,
            ContactName = supplier.ContactName,
            ContactEmail = supplier.ContactEmail,
            OpeningTimes = supplier.OpeningTimes,
            OpeningTimesMonday = supplier.OpeningTimesMonday,
            OpeningTimesTuesday = supplier.OpeningTimesTuesday,
            OpeningTimesWednesday = supplier.OpeningTimesWednesday,
            OpeningTimesThursday = supplier.OpeningTimesThursday,
            OpeningTimesFriday = supplier.OpeningTimesFriday,
            OpeningTimesSaturday = supplier.OpeningTimesSaturday,
            OpeningTimesSunday = supplier.OpeningTimesSunday,
            IsActive = supplier.IsActive,
            OriginalIsActive = supplier.IsActive,
            Tag1 = supplier.Tag1,
            Tag2 = supplier.Tag2,
            Tag3 = supplier.Tag3,
            Tag4 = supplier.Tag4,
            ConfirmationCode = supplier.ConfirmationCode,
            EmailNotificationsEnabled = supplier.EmailNotificationsEnabled
        };
    }

    public Supplier ToSupplier() {
        var result = new Supplier();
        ApplyTo(result);
        return result;
    }

    public void ApplyTo(Supplier supplier) {
        supplier.Name = Name;
        supplier.Address = Address;
        supplier.Description = Description;
        supplier.ContactName = ContactName;
        supplier.ContactEmail = ContactEmail;
        supplier.OpeningTimes = OpeningTimes;
        supplier.OpeningTimesMonday = OpeningTimesMonday;
        supplier.OpeningTimesTuesday = OpeningTimesTuesday;
        supplier.OpeningTimesWednesday = OpeningTimesWednesday;
        supplier.OpeningTimesThursday = OpeningTimesThursday;
        supplier.OpeningTimesFriday = OpeningTimesFriday;
        supplier.OpeningTimesSaturday = OpeningTimesSaturday;
        supplier.OpeningTimesSunday = OpeningTimesSunday;
        supplier.IsActive = IsActive;
        supplier.Tag1 = Tag1;
        supplier.Tag2 = Tag2;
        supplier.Tag3 = Tag3;
        supplier.Tag4 = Tag4;
        supplier.ConfirmationCode = ConfirmationCode;
        supplier.EmailNotificationsEnabled = EmailNotificationsEnabled;
    }
}
