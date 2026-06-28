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

    public async Task<IActionResult> Visits(string? filter = null, int page = 1, string? sort = null, string? dir = null) {
        var query = context.Visits
            .Include(v => v.Booking)
            .Include(v => v.Supplier)
            .AsQueryable();

        query = filter switch {
            "claimed" => query.Where(v => v.IsClaimed),
            "unclaimed" => query.Where(v => !v.IsClaimed),
            "reimbursed" => query.Where(v => v.IsSupplierReimbursed),
            "unreimbursed" => query.Where(v => v.IsClaimed && !v.IsSupplierReimbursed),
            _ => query
        };

        int totalCount = await query.CountAsync();
        int totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / PAGE_SIZE) : 1;
        page = Math.Clamp(page, 1, totalPages);

        string sortColumn = sort ?? AdminVisitsVM.SORT_DATE;
        bool descending = dir != null ? dir == "desc" : sortColumn == AdminVisitsVM.SORT_DATE;

        var orderedQuery = ApplyVisitsSorting(query, sortColumn, descending);

        var visits = await orderedQuery
            .Skip((page - 1) * PAGE_SIZE)
            .Take(PAGE_SIZE)
            .ToListAsync();

        return View(new AdminVisitsVM {
            Visits = visits,
            ActiveFilter = filter,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = PAGE_SIZE,
            SortColumn = sortColumn,
            SortDescending = descending
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkReimbursed(int id, string? filter = null, int? page = null, string? sort = null, string? dir = null) {
        var visit = await context.Visits.FindAsync(id);
        if (visit == null) {
            return NotFound();
        }

        visit.IsSupplierReimbursed = !visit.IsSupplierReimbursed;
        visit.ReimbursedAt = visit.IsSupplierReimbursed ? DateTime.UtcNow : null;

        bool isAjax = string.Equals(Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

        try {
            await context.SaveChangesAsync();
            Log.Information("Visit {VisitId} reimbursement toggled to {IsReimbursed}", id, visit.IsSupplierReimbursed);

            if (isAjax) {
                return Json(new {
                    success = true,
                    isReimbursed = visit.IsSupplierReimbursed,
                    reimbursedAt = visit.ReimbursedAt?.ToString("o"),
                    message = visit.IsSupplierReimbursed ? "Marked as reimbursed." : "Reimbursement undone."
                });
            }

            return RedirectToAction(nameof(Visits), new { filter, page, sort, dir });
        } catch (Exception ex) {
            Log.Error(ex, "Failed to update reimbursement status for visit {VisitId}", id);

            if (isAjax) {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "Failed to update reimbursement status. Please try again." });
            }

            return RedirectToAction(nameof(Visits), new { filter, page, sort, dir });
        }
    }

    private static IOrderedQueryable<Visit> ApplyVisitsSorting(IQueryable<Visit> query, string sortColumn, bool descending) {
        return sortColumn switch {
            AdminVisitsVM.SORT_CUSTOMER => descending
                ? query.OrderByDescending(v => v.Booking.CustomerName ?? "")
                : query.OrderBy(v => v.Booking.CustomerName ?? ""),
            AdminVisitsVM.SORT_REF => descending
                ? query.OrderByDescending(v => v.Booking.PurchaseCode)
                : query.OrderBy(v => v.Booking.PurchaseCode),
            AdminVisitsVM.SORT_SUPPLIER => descending
                ? query.OrderByDescending(v => v.Supplier.Name)
                : query.OrderBy(v => v.Supplier.Name),
            AdminVisitsVM.SORT_GUESTS => descending
                ? query.OrderByDescending(v => v.Booking.TotalGuests)
                : query.OrderBy(v => v.Booking.TotalGuests),
            _ => descending
                ? query.OrderByDescending(v => v.Booking.TourDate)
                : query.OrderBy(v => v.Booking.TourDate)
        };
    }

    public async Task<IActionResult> Suppliers(string? filter = null, string? sort = null, string? dir = null) {
        var query = context.Suppliers.AsQueryable();

        query = filter switch {
            "active" => query.Where(s => s.IsActive),
            "inactive" => query.Where(s => !s.IsActive),
            _ => query
        };

        string sortColumn = sort ?? AdminSuppliersVM.SORT_NAME;
        bool descending = dir != null ? dir == "desc" : sortColumn is AdminSuppliersVM.SORT_ACTIVE or AdminSuppliersVM.SORT_EMAILS;

        var suppliers = await ApplySuppliersSorting(query, sortColumn, descending);

        return View(new AdminSuppliersVM {
            Suppliers = suppliers,
            ActiveFilter = filter,
            SortColumn = sortColumn,
            SortDescending = descending
        });
    }

    private static async Task<List<Supplier>> ApplySuppliersSorting(IQueryable<Supplier> query, string sortColumn, bool descending) {
        var orderedQuery = sortColumn switch {
            AdminSuppliersVM.SORT_CONTACT => descending
                ? query.OrderByDescending(s => s.ContactName).ThenByDescending(s => s.ContactEmail)
                : query.OrderBy(s => s.ContactName).ThenBy(s => s.ContactEmail),
            AdminSuppliersVM.SORT_ACTIVE => descending
                ? query.OrderByDescending(s => s.IsActive).ThenBy(s => s.Name)
                : query.OrderBy(s => s.IsActive).ThenBy(s => s.Name),
            AdminSuppliersVM.SORT_EMAILS => descending
                ? query.OrderByDescending(s => s.EmailNotificationsEnabled).ThenBy(s => s.Name)
                : query.OrderBy(s => s.EmailNotificationsEnabled).ThenBy(s => s.Name),
            _ => descending
                ? query.OrderByDescending(s => s.Name)
                : query.OrderBy(s => s.Name)
        };

        var suppliers = await orderedQuery.ToListAsync();
        return suppliers;
    }

    public IActionResult CreateSupplier() {
        return View(new AdminSupplierFormVM());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSupplier(AdminSupplierFormVM model) {
        if (!ModelState.IsValid) {
            return View(model);
        }

        var supplier = model.ToSupplier();

        try {
            context.Suppliers.Add(supplier);
            await context.SaveChangesAsync();
            Log.Information("Supplier {SupplierId} created", supplier.Id);
            return RedirectToAction(nameof(Suppliers));
        } catch (Exception ex) {
            Log.Error(ex, "Failed to create supplier");
            ModelState.AddModelError(string.Empty, "Failed to create supplier. Please try again.");
            return View(model);
        }
    }

    public async Task<IActionResult> EditSupplier(int id, string? filter = null) {
        var supplier = await context.Suppliers.FindAsync(id);
        if (supplier == null) {
            return NotFound();
        }

        var result = AdminSupplierFormVM.FromSupplier(supplier);
        ViewData["SuppliersFilter"] = filter;
        return View(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSupplier(int id, AdminSupplierFormVM model, string? filter = null) {
        if (id != model.Id) {
            return BadRequest();
        }

        var supplier = await context.Suppliers.FindAsync(id);
        if (supplier == null) {
            return NotFound();
        }

        if (!ModelState.IsValid) {
            ViewData["SuppliersFilter"] = filter;
            return View(model);
        }

        model.ApplyTo(supplier);

        try {
            await context.SaveChangesAsync();
            Log.Information("Supplier {SupplierId} updated", supplier.Id);
            return RedirectToAction(nameof(Suppliers), new { filter });
        } catch (Exception ex) {
            Log.Error(ex, "Failed to update supplier {SupplierId}", supplier.Id);
            ModelState.AddModelError(string.Empty, "Failed to update supplier. Please try again.");
            ViewData["SuppliersFilter"] = filter;
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableSupplier(int id) {
        var supplier = await context.Suppliers.FindAsync(id);
        if (supplier == null) {
            return NotFound();
        }

        supplier.IsActive = false;

        try {
            await context.SaveChangesAsync();
            Log.Information("Supplier {SupplierId} deactivated", supplier.Id);
        } catch (Exception ex) {
            Log.Error(ex, "Failed to deactivate supplier {SupplierId}", supplier.Id);
        }

        return RedirectToAction(nameof(Suppliers));
    }

    public async Task<IActionResult> Dashboard() {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var result = new AdminDashboardVM {
            PastBookings = await context.Bookings.CountAsync(b => !b.IsCancelled && b.TourDate < today),
            TodayBookings = await context.Bookings.CountAsync(b => !b.IsCancelled && b.TourDate >= today && b.TourDate < tomorrow),
            UpcomingBookings = await context.Bookings.CountAsync(b => !b.IsCancelled && b.TourDate >= tomorrow),
            VisitsAwaitingReimbursement = await context.Visits.CountAsync(v => v.IsClaimed && !v.IsSupplierReimbursed),
            ActiveSuppliers = await context.Suppliers.CountAsync(s => s.IsActive)
        };
        return View(result);
    }
}
