namespace FoodTour.ViewModels;

public class AdminDashboardVM {
    public int PastBookings { get; set; }
    public int TodayBookings { get; set; }
    public int UpcomingBookings { get; set; }
    public int ClaimedVisits { get; set; }
    public int UnclaimedVisits { get; set; }
    public int ActiveSuppliers { get; set; }
}
