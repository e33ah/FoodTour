namespace FoodTour.Models;

public class AdminDashboardVM {
    public int PastBookings { get; set; }
    public int TodayBookings { get; set; }
    public int UpcomingBookings { get; set; }
    public int VisitsAwaitingReimbursement { get; set; }
    public int ActiveSuppliers { get; set; }
}
