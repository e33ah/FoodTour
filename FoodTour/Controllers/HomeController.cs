namespace FoodTour.Controllers;

public class HomeController(FoodTourContext context) : Controller {
    private const int MAX_SUPPLIER_SELECTIONS = 5;
    private readonly QrImageService qrImageService = new();

    public async Task<IActionResult> Index() {
        var model = await CreateFoodTourViewModel();

        if (model.Suppliers.Count == 0) {
            ViewBag.NoSuppliersMessage = "No tasting experiences are currently available for the tour. Please check back later.";
        } else if (model.Suppliers.Count < MAX_SUPPLIER_SELECTIONS) {
            ViewBag.InsufficientSuppliersWarning = GetInsufficientSuppliersMessage(model.Suppliers.Count);
            Log.Warning("Insufficient active suppliers: {SupplierCount} found, {RequiredCount} required.", model.Suppliers.Count, MAX_SUPPLIER_SELECTIONS);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(DateTime? tourDate, List<int> selectedSuppliers, string customerEmail, string purchaseCode) {
        if (!tourDate.HasValue) {
            ModelState.AddModelError(nameof(FoodTourVM.TourDate), "Please select a date for your tour.");
        } else if (!IsValidTourDate(tourDate.Value)) {
            ModelState.AddModelError(nameof(FoodTourVM.TourDate), "Tours are only available Monday to Saturday between March and October, and only for future dates.");
        }

        if (string.IsNullOrWhiteSpace(customerEmail)) {
            ModelState.AddModelError(nameof(FoodTourVM.CustomerEmail), "Please enter your email address.");
        } else if (!new EmailAddressAttribute().IsValid(customerEmail)) {
            ModelState.AddModelError(nameof(FoodTourVM.CustomerEmail), "Please enter a valid email address.");
        }

        if (string.IsNullOrWhiteSpace(purchaseCode)) {
            ModelState.AddModelError(nameof(FoodTourVM.PurchaseCode), "Please enter your purchase code.");
        }
        // TODO: FT-12 Validate purchase code

        if (selectedSuppliers.Count != MAX_SUPPLIER_SELECTIONS) {
            ModelState.AddModelError(string.Empty, $"You must select {MAX_SUPPLIER_SELECTIONS} tasting experiences for your tour.");
        }

        if (!ModelState.IsValid) {
            var model = await CreateFoodTourViewModel();
            model.TourDate = tourDate;
            return View(model);
        }
        var bookingSummaryModel = new BookingSummaryVM {
            TourDate = tourDate!.Value,
            CustomerEmail = customerEmail,
            PurchaseCode = purchaseCode,
            SelectedSupplierIds = selectedSuppliers,
            SelectedSuppliers = await context.Suppliers.Where(s => selectedSuppliers.Contains(s.Id)).ToListAsync()
        };
        Log.Information("User submitted tour request for date {TourDate:dd/MM/yyyy} with {SelectedCount} suppliers.", bookingSummaryModel.TourDate, selectedSuppliers.Count);
        return RedirectToAction("BookingSummary", bookingSummaryModel);
    }

    [HttpGet]
    public async Task<IActionResult> BookingSummary(BookingSummaryVM bookingSummary) {
        if (bookingSummary == null) {
            return RedirectToAction("Index");
        }

        bookingSummary.SelectedSupplierIds ??= [];

        if (bookingSummary.SelectedSuppliers == null || bookingSummary.SelectedSuppliers.Count == 0) {
            if (bookingSummary.SelectedSupplierIds.Count > 0) {
                bookingSummary.SelectedSuppliers = await context.Suppliers
                    .Where(s => bookingSummary.SelectedSupplierIds.Contains(s.Id))
                    .ToListAsync();
            } else {
                bookingSummary.SelectedSuppliers = [];
            }
        }

        return View(bookingSummary);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ConfirmBookingSummary(BookingSummaryVM bookingSummary) {
        if (bookingSummary == null) {
            Log.Warning("ConfirmBookingSummary called with null model.");
            return RedirectToAction("Index");
        }

        if (!ModelState.IsValid) {
            return View("BookingSummary", bookingSummary);
        }

        var confirmationModel = new BookingConfirmationVM {
            TourDate = bookingSummary.TourDate,
            CustomerEmail = bookingSummary.CustomerEmail,
            PurchaseCode = bookingSummary.PurchaseCode,
            SelectedSupplierIds = bookingSummary.SelectedSupplierIds
        };

        TempData["Confirmation"] = JsonSerializer.Serialize(confirmationModel);

        Log.Information("User confirmed booking for {CustomerEmail} on {TourDate:dd/MM/yyyy}.", bookingSummary.CustomerEmail, bookingSummary.TourDate);

        return RedirectToAction("BookingConfirmation");
    }

    [HttpGet]
    public async Task<IActionResult> BookingConfirmation() {
        object? bookingIdObj = TempData.Peek("BookingId"); // Peek to retain TempData for subsequent requests
        if (bookingIdObj == null) {
            return RedirectToAction("Index");
        }

        int bookingId;
        if (bookingIdObj is int bookingIdInt) {
            bookingId = bookingIdInt;
        } else if (bookingIdObj is string s && int.TryParse(s, out int parsedId)) {
            bookingId = parsedId;

        } else {
            Log.Warning("Confirmation action received unexpected BookingId type: {Type}.", bookingIdObj.GetType().FullName);
            return RedirectToAction("Index");
        }

        var booking = await context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking == null) {
            Log.Warning("Confirmation action could not find booking with id {BookingId}.", bookingId);
            return RedirectToAction("Index");
        }

        var model = new BookingConfirmationVM() {
            TourDate = booking.TourDate,
            CustomerEmail = booking.CustomerEmail,
            PurchaseCode = booking.PurchaseCode,
            SelectedSupplierIds = booking.SelectedSupplierIds,
            BookingId = booking.Id,
            QrToken = booking.QrToken
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveConfirmedBooking([FromBody] BookingConfirmationVM? confirmation = null) {
        if (confirmation == null) {
            Log.Warning("SaveConfirmedBooking called with null or invalid request body.");
            return BadRequest("Invalid request.");
        }

        var selectedSupplierIds = confirmation.SelectedSupplierIds;
        int actualSupplierSelectionCount = selectedSupplierIds?.Count ?? 0;
        if (selectedSupplierIds == null || actualSupplierSelectionCount != MAX_SUPPLIER_SELECTIONS) {
            Log.Warning("Invalid supplier selection in confirmation data: {ActualCount}, expected: {ExpectedCount}", actualSupplierSelectionCount, MAX_SUPPLIER_SELECTIONS);
            string result = $"Invalid supplier selection. Expected {MAX_SUPPLIER_SELECTIONS} supplier selections, but received {actualSupplierSelectionCount}.";
            return BadRequest(result);
        }

        // Validate that all supplier IDs exist in the database before creating BookingSupplier records
        var existingSupplierIds = await context.Suppliers
            .Where(s => selectedSupplierIds.Contains(s.Id) && s.IsActive)
            .Select(s => s.Id)
            .ToListAsync();

        if (existingSupplierIds.Count != selectedSupplierIds.Count) {
            Log.Warning("One or more selected supplier IDs do not exist in the database. Provided: {ProvidedIds}, Existing: {ExistingIds}", selectedSupplierIds, existingSupplierIds);
            return BadRequest("One or more selected suppliers are invalid.");
        }

        var booking = new Booking {
            TourDate = confirmation.TourDate,
            CustomerEmail = confirmation.CustomerEmail,
            PurchaseCode = confirmation.PurchaseCode,
            SelectedSupplierIds = selectedSupplierIds,
            CreatedAt = DateTime.UtcNow
        };

        booking.BookingSuppliers = [.. selectedSupplierIds.Select(id =>
            new BookingSupplier {
                Booking = booking,
                SupplierId = id
            })];

        var visits = selectedSupplierIds.Select(id =>
            new Visit {
                Booking = booking,
                SupplierId = id,
                IsClaimed = false
            }).ToList();

        try {
            await context.Bookings.AddAsync(booking);
            await context.Visits.AddRangeAsync(visits);
            await context.SaveChangesAsync();

            Log.Information("Saved booking id {BookingId} for {CustomerEmail} on {TourDate:dd/MM/yyyy}.", booking.Id, booking.CustomerEmail, booking.TourDate);

            // Store booking identifier so the BookingConfirmation view can load full details from the database
            TempData["BookingId"] = booking.Id;
            TempData["BookingSaved"] = "Your booking has been saved.";

            return Ok(new { success = true, nextAction = "Confirmation" });

        } catch (Exception ex) {
            const string BOOKING_SAVE_ERROR_MESSAGE = "Unable to save your booking due to a system error. Please try again or contact support if the problem persists.";
            Log.Error(ex, "Failed to save booking to database.");
            TempData["BookingSaveError"] = BOOKING_SAVE_ERROR_MESSAGE;

            return StatusCode(500, new { success = false, nextAction = "BookingSummary", message = BOOKING_SAVE_ERROR_MESSAGE });
        }
    }

    [HttpGet]
    public async Task<IActionResult> SupplierVisit(Guid qrToken) {
        if (qrToken == Guid.Empty) {
            Log.Warning("SupplierVisit action called with invalid token: {QrToken}.", qrToken);
            return RedirectToAction("Error");
        }

        var booking = await context.Bookings
            .Include(b => b.BookingSuppliers)
            .ThenInclude(bs => bs.Supplier)
            .FirstOrDefaultAsync(b => b.QrToken == qrToken && !b.IsCancelled);

        if (booking == null || booking.BookingSuppliers == null) {
            Log.Warning("Booking or BookingSuppliers is null for token {QrToken}.", qrToken);
            return RedirectToAction("Error");
        }

        var claimedSupplierIds = await context.Visits
            .Where(v => v.BookingId == booking.Id && v.IsClaimed)
            .Select(v => v.SupplierId)
            .ToListAsync();

        var unclaimedSuppliers = booking.BookingSuppliers
            .Where(bs => !claimedSupplierIds.Contains(bs.SupplierId))
            .Select(bs => bs.Supplier)
            .ToList();

        Log.Debug("Found {ClaimedCount} claimed suppliers and {UnclaimedCount} unclaimed suppliers for bookingId {BookingId}.",
            claimedSupplierIds.Count, unclaimedSuppliers.Count, booking.Id);

        if (unclaimedSuppliers.Count == 0) {
            Log.Information("All suppliers already visited for booking {BookingId}. Redirecting to confirmation.", booking.Id);
            TempData["VisitMessage"] = "You have already confirmed your visit to all suppliers for this tour. Thank you!";
            //TODO FT-22 Redirect to a dedicated "Tour Completed" page that thanks the user and provides any next steps or feedback options.
            return RedirectToAction("Error");
        }

        var model = new SupplierVisitVM() {
            BookingId = booking.Id,
            QrToken = booking.QrToken,
            Suppliers = unclaimedSuppliers,
            TourDate = booking.TourDate,
            CustomerEmail = booking.CustomerEmail,
        };
        return View(model);
    }

    public record RecordVisitDTO(Guid QrToken, int SelectedSupplierId);

    [HttpPut]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordVisit([FromBody] RecordVisitDTO? payload) {
        if (payload == null || payload.SelectedSupplierId <= 0 || payload.QrToken == Guid.Empty) {
            Log.Warning("RecordVisit called with invalid parameters.");
            return BadRequest(new { error = "Invalid booking or supplier information." });
        }

        var booking = await context.Bookings.FirstOrDefaultAsync(b => b.QrToken == payload.QrToken && !b.IsCancelled);
        if (booking == null) {
            Log.Warning("RecordVisit could not find active booking with QrToken: {QrToken}.", payload.QrToken);
            return BadRequest(new { error = "Booking not found." });
        }

        var visit = await context.Visits
            .FirstOrDefaultAsync(v => v.BookingId == booking.Id && v.SupplierId == payload.SelectedSupplierId);

        if (visit == null) {
            Log.Warning("RecordVisit could not find visit for booking id {BookingId} and supplier id {SupplierId}.", booking.Id, payload.SelectedSupplierId);
            return BadRequest(new { error = "Visit not found for this booking and supplier." });
        }

        if (visit.IsClaimed) {
            Log.Warning("RecordVisit called for already claimed visit: booking id {BookingId}, supplier id {SupplierId}.", booking.Id, payload.SelectedSupplierId);
            return BadRequest(new { error = "This visit has already been confirmed." });
        }

        visit.IsClaimed = true;
        visit.ClaimedAt = DateTime.UtcNow;
        //TODO: Review if this code could have race condition bug if multiple requests are made at same time

        try {
            await context.SaveChangesAsync();
            Log.Information("Visit recorded for booking id {BookingId} and supplier id {SupplierId}.", booking.Id, payload.SelectedSupplierId);
            return Ok(new { success = true });
        } catch (Exception ex) {
            Log.Error(ex, "Failed to save visit record for booking id {BookingId} and supplier id {SupplierId}.", booking.Id, payload.SelectedSupplierId);
            return StatusCode(500, new { error = "Unable to record visit due to a system error." });
        }
    }

    [HttpGet("[controller]/qr")]
    public async Task<IActionResult> QrImage(Guid qrToken) {
        if (qrToken == Guid.Empty) {
            Log.Warning("QrImage action called with invalid token: {QrToken}.", qrToken);
            return BadRequest("QR Token is required.");
        }

        var booking = await context.Bookings.FirstOrDefaultAsync(b => b.QrToken == qrToken && !b.IsCancelled);
        if (booking == null) {
            Log.Warning("QrImage action could not find booking with token {QrToken}.", qrToken);
            return NotFound("Booking not found or cancelled.");
        }

        string? url = Url.Action("SupplierVisit", "Home", new { qrToken }, Request.Scheme);

        if (string.IsNullOrEmpty(url)) {
            Log.Error("Failed to generate URL for QR code with token {QrToken}.", qrToken);
            return BadRequest("Unable to generate QR code due to a system error.");
        }

        byte[] qrCodeBytes = qrImageService.GenerateQrForUrl(url);

        return File(qrCodeBytes, "image/png");
    }

    public IActionResult VisitConfirmation() {
        return View();
    }

    public IActionResult Privacy() {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private async Task<FoodTourVM> CreateFoodTourViewModel() {
        List<Supplier> suppliers;
        try {
            suppliers = await context.Suppliers.Where(s => s.IsActive).ToListAsync();
            Log.Debug("Retrieved {SupplierCount} active suppliers from the database.", suppliers.Count);
        } catch (Exception ex) {
            Log.Error(ex, "Failed to retrieve active suppliers from the database.");
            suppliers = [];
        }
        return new FoodTourVM {
            Suppliers = suppliers,
            MaxSelections = MAX_SUPPLIER_SELECTIONS
        };
    }

    private static string GetInsufficientSuppliersMessage(int supplierCount) {
        string experienceText = supplierCount == 1 ? "tasting experience is" : "tasting experiences are";
        return $"Only {supplierCount} {experienceText} currently available. You need at least {MAX_SUPPLIER_SELECTIONS} to create a tour. Please check back later.";
    }

    private static bool IsValidTourDate(DateTime date) {
        var today = DateTime.Today;
        if (date <= today) {
            return false;
        }
        if (date.DayOfWeek == DayOfWeek.Sunday) {
            return false;
        }
        if (date.Month < 3 || date.Month > 10) {
            return false;
        }
        return true;
    }
}
