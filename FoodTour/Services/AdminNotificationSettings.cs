namespace FoodTour.Services;

public class AdminNotificationSettings {
    public string AdminEmail { get; set; } = string.Empty;
    public bool NotifyOnVisitRecorded { get; set; }
}
