namespace FoodTour.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AdminController(IAdminAccessService adminAccessService, IConfiguration configuration, FoodTourContext context) : Controller {
    private const int PAGE_SIZE = 25;

    private bool GoogleConfigured =>
        !string.IsNullOrEmpty(configuration["Authentication:Google:ClientId"]) &&
        !string.IsNullOrEmpty(configuration["Authentication:Google:ClientSecret"]);

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) {
        if (User.Identity?.IsAuthenticated == true) {
            return RedirectToAction(nameof(Dashboard));
        }

        ViewData["ReturnUrl"] = returnUrl;
        ViewData["GoogleConfigured"] = GoogleConfigured;
        return View();
    }

    [AllowAnonymous]
    public IActionResult LoginWithGoogle(string? returnUrl = null) {
        if (!GoogleConfigured) {
            return RedirectToAction(nameof(Login));
        }

        string redirectUrl = Url.Action(nameof(GoogleCallback), new { returnUrl })!;
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        properties.SetParameter("prompt", "select_account");
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback(string? returnUrl = null) {
        string? email = User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrEmpty(email) || !adminAccessService.IsAllowed(email)) {
            Log.Warning("Admin access denied for {Email}", email ?? "(none)");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(AccessDenied));
        }

        Log.Information("Admin login successful for {Email}", email);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Dashboard));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() {
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout() {
        Log.Information("Admin logout for {Email}", User.FindFirstValue(ClaimTypes.Email));
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    public async Task<IActionResult> Bookings(string? filter = null, int page = 1, string? sort = null, string? dir = null) {
        var today = DateTime.UtcNow.Date;
        var query = context.Bookings.AsQueryable();

        query = filter switch {
            // "upcoming" includes today, unlike the Dashboard tile which only counts tomorrow onwards
            "upcoming" => query.Where(b => !b.IsCancelled && b.TourDate >= today),
            "past" => query.Where(b => !b.IsCancelled && b.TourDate < today),
            "cancelled" => query.Where(b => b.IsCancelled),
            _ => query
        };

        int totalCount = await query.CountAsync();
        int totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / PAGE_SIZE) : 1;
        page = Math.Clamp(page, 1, totalPages);

        string sortColumn = sort ?? AdminBookingsVM.SORT_DATE;
        // Default direction: descending for date (newest first), ascending for all other columns
        bool descending = dir != null ? dir == "desc" : sortColumn == AdminBookingsVM.SORT_DATE;

        var orderedQuery = sortColumn switch {
            AdminBookingsVM.SORT_CUSTOMER => descending ? query.OrderByDescending(b => b.CustomerName ?? "") : query.OrderBy(b => b.CustomerName ?? ""),
            AdminBookingsVM.SORT_REF => descending ? query.OrderByDescending(b => b.PurchaseCode) : query.OrderBy(b => b.PurchaseCode),
            AdminBookingsVM.SORT_GUESTS => descending ? query.OrderByDescending(b => b.TotalGuests) : query.OrderBy(b => b.TotalGuests),
            _ => descending ? query.OrderByDescending(b => b.TourDate) : query.OrderBy(b => b.TourDate)
        };

        var bookings = await orderedQuery
            .Skip((page - 1) * PAGE_SIZE)
            .Take(PAGE_SIZE)
            .ToListAsync();

        return View(new AdminBookingsVM {
            Bookings = bookings,
            ActiveFilter = filter,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = PAGE_SIZE,
            SortColumn = sortColumn,
            SortDescending = descending
        });
    }

    public async Task<IActionResult> BookingDetail(int id) {
        var booking = await context.Bookings.FindAsync(id);
        if (booking == null) {
            return NotFound();
        }

        var selectedSuppliers = await context.Suppliers
            .Where(s => context.BookingSuppliers.Any(bs => bs.BookingId == id && bs.SupplierId == s.Id))
            .OrderBy(s => s.Name)
            .ToListAsync();

        var visits = await context.Visits
            .Include(v => v.Supplier)
            .Where(v => v.BookingId == id)
            .ToListAsync();

        return View(new AdminBookingDetailVM {
            Booking = booking,
            Visits = visits,
            SelectedSuppliers = selectedSuppliers
        });
    }

    public async Task<IActionResult> Dashboard() {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var result = new AdminDashboardVM {
            PastBookings = await context.Bookings.CountAsync(b => !b.IsCancelled && b.TourDate < today),
            TodayBookings = await context.Bookings.CountAsync(b => !b.IsCancelled && b.TourDate >= today && b.TourDate < tomorrow),
            UpcomingBookings = await context.Bookings.CountAsync(b => !b.IsCancelled && b.TourDate >= tomorrow),
            ClaimedVisits = await context.Visits.CountAsync(v => v.IsClaimed),
            UnclaimedVisits = await context.Visits.CountAsync(v => !v.IsClaimed),
            ActiveSuppliers = await context.Suppliers.CountAsync(s => s.IsActive)
        };
        return View(result);
    }
}
