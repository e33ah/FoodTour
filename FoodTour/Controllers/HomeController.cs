namespace FoodTour.Controllers;

public class HomeController(FoodTourContext context) : Controller {
    private const int MAX_SUPPLIER_SELECTIONS = 5;

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
            SelectedSupplierIds = booking.SelectedSupplierIds
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

        int actualSupplierSelectionCount = confirmation.SelectedSupplierIds?.Count ?? 0;
        if (confirmation.SelectedSupplierIds == null || actualSupplierSelectionCount != MAX_SUPPLIER_SELECTIONS) {
            Log.Warning("Invalid supplier selection in confirmation data: {ActualCount}, expected: {ExpectedCount}", actualSupplierSelectionCount, MAX_SUPPLIER_SELECTIONS);
            string result = $"Invalid supplier selection. Expected {MAX_SUPPLIER_SELECTIONS} supplier selections, but received {actualSupplierSelectionCount}.";
            return BadRequest(result);
        }

        // Validate that all supplier IDs exist in the database before creating BookingSupplier records
        var existingSupplierIds = await context.Suppliers
            .Where(s => confirmation.SelectedSupplierIds.Contains(s.Id) && s.IsActive)
            .Select(s => s.Id)
            .ToListAsync();

        if (existingSupplierIds.Count != confirmation.SelectedSupplierIds.Count) {
            Log.Warning("One or more selected supplier IDs do not exist in the database. Provided: {ProvidedIds}, Existing: {ExistingIds}", confirmation.SelectedSupplierIds, existingSupplierIds);
            return BadRequest("One or more selected suppliers are invalid.");
        }

        var booking = new Booking {
            TourDate = confirmation.TourDate,
            CustomerEmail = confirmation.CustomerEmail,
            PurchaseCode = confirmation.PurchaseCode,
            SelectedSupplierIds = confirmation.SelectedSupplierIds,
            CreatedAt = DateTime.UtcNow
        };

        booking.BookingSuppliers = [.. existingSupplierIds.Select(id =>
            new BookingSupplier {
                Booking = booking,
                SupplierId = id
            })];

        try {
            await context.Bookings.AddAsync(booking);
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

    public IActionResult SupplierVisit(int bookingId) {
        if (bookingId <= 0) {
#if DEBUG
            bookingId = 17; // For test purposes only, needs removing. FT-14
#else
            Log.Warning("SupplierVisit action called with invalid bookingId: {BookingId}.", bookingId);
            return RedirectToAction("Error");
#endif
        }
        var booking = context.Bookings
            .Include(b => b.BookingSuppliers)
            .ThenInclude(bs => bs.Supplier)
            .FirstOrDefault(b => b.Id == bookingId);

        if (booking == null) {
            Log.Warning("SupplierVisit action could not find booking with id {BookingId}.", bookingId);
            return RedirectToAction("Error");
        }
        var model = new SupplierVisitVM() {
            Suppliers = [.. booking.BookingSuppliers.Select(bs => bs.Supplier)],
            TourDate = booking.TourDate,
            CustomerEmail = booking.CustomerEmail,
        };
        return View(model);
    }

    public record RecordVisitDTO(int SelectedSupplierId);

    [HttpPut]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordVisit([FromBody] RecordVisitDTO? payload)
    {
        if (payload == null || payload.SelectedSupplierId <= 0)
        {
            Log.Warning("RecordVisit called without a selectedSupplierId.");
            return BadRequest(new { error = "No supplier selected." });
        }

        int selectedSupplierId = payload.SelectedSupplierId;

        bool supplierExists = await context.Suppliers.AnyAsync(s => s.Id == selectedSupplierId);
        if (!supplierExists)
        {
            Log.Warning("RecordVisit called with invalid supplier id {SupplierId}.", selectedSupplierId);
            return BadRequest(new { error = "Invalid supplier selected." });
        }

        // TODO: FT-18 Validate that the selected supplier ID is valid and associated with the user's booking and save to db

        return Ok(new { success = true });
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
