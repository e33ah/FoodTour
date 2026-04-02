namespace FoodTour.Controllers;

public class HomeController(FoodTourContext context, IEmailSender emailSender, BookingEmailService bookingEmailService, IOptions<SupportSettings> supportOptions, IBookingValidationService bookingValidationService) : Controller {
    private const int MAX_SUPPLIER_SELECTIONS = 5;
    private readonly QrImageService qrImageService = new();
    private readonly SupportSettings supportSettings = supportOptions.Value;

    public async Task<IActionResult> Index(Guid? qrToken) {
        var model = await CreateFoodTourViewModel();

        if (model.Suppliers.Count == 0) {
            ViewBag.NoSuppliersMessage = "No tasting experiences are currently available for the tour. Please check back later.";
        } else if (model.Suppliers.Count < MAX_SUPPLIER_SELECTIONS) {
            ViewBag.InsufficientSuppliersWarning = GetInsufficientSuppliersMessage(model.Suppliers.Count);
            Log.Warning("Insufficient active suppliers: {SupplierCount} found, {RequiredCount} required.", model.Suppliers.Count, MAX_SUPPLIER_SELECTIONS);
        }

        await PopulateFormFromQrTokenAsync(model, qrToken);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(int bookingId, List<int> selectedSuppliers) {
        // Validate that the booking ID is provided
        if (bookingId <= 0) {
            Log.Warning("Index POST called with invalid or missing booking ID: {BookingId}", bookingId);
            ModelState.AddModelError(string.Empty, "Your booking could not be validated. Please enter your email and booking reference.");
            return await CreateIndexViewWithErrorsAsync(selectedSuppliers);
        }

        // Retrieve the booking from the database
        var booking = await context.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && !b.IsCancelled);

        if (booking == null) {
            Log.Warning("Index POST could not find active booking with ID: {BookingId}", bookingId);
            ModelState.AddModelError(string.Empty, "Your booking could not be found or has been cancelled. Please verify your details.");
            return await CreateIndexViewWithErrorsAsync(selectedSuppliers);
        }

        // Validate supplier selections
        var validationRequest = new BookingValidationRequest {
            TourDate = booking.TourDate,
            CustomerEmail = booking.CustomerEmail,
            SelectedSupplierIds = selectedSuppliers
        };

        var validationResult = await bookingValidationService.ValidateAsync(validationRequest);

        if (!validationResult.IsValid) {
            MapValidationErrorsToModelState(validationResult);
            return await CreateIndexViewWithErrorsAsync(selectedSuppliers);
        }

        // Validate that all supplier IDs exist in the database
        var existingSupplierIds = await context.Suppliers
            .Where(s => selectedSuppliers.Contains(s.Id) && s.IsActive)
            .Select(s => s.Id)
            .ToListAsync();

        if (existingSupplierIds.Count != selectedSuppliers.Count) {
            Log.Warning("Booking {BookingId}: One or more selected supplier IDs do not exist in the database. Provided: {ProvidedIds}, Existing: {ExistingIds}", bookingId, selectedSuppliers, existingSupplierIds);
            ModelState.AddModelError(string.Empty, "One or more selected suppliers are invalid. Please refresh and try again.");
            return await CreateIndexViewWithErrorsAsync(selectedSuppliers);
        }

        // Update the booking with new supplier selections
        booking.SelectedSupplierIds = selectedSuppliers;

        // Remove old BookingSuppliers
        var oldBookingSuppliers = await context.BookingSuppliers
            .Where(bs => bs.BookingId == booking.Id)
            .ToListAsync();
        context.BookingSuppliers.RemoveRange(oldBookingSuppliers);

        // Add new BookingSuppliers
        booking.BookingSuppliers = [.. selectedSuppliers.Select(id =>
            new BookingSupplier {
                Booking = booking,
                SupplierId = id
            })];

        // Remove old Visits
        var oldVisits = await context.Visits
            .Where(v => v.BookingId == booking.Id)
            .ToListAsync();
        context.Visits.RemoveRange(oldVisits);

        // Add new Visits
        var newVisits = selectedSuppliers.Select(id =>
            new Visit {
                Booking = booking,
                SupplierId = id,
                IsClaimed = false
            }).ToList();

        await context.Visits.AddRangeAsync(newVisits);

        try {
            await context.SaveChangesAsync();
            Log.Information("Booking {BookingId}: Updated for {CustomerEmail} on {TourDate:dd/MM/yyyy} with {SupplierCount} suppliers.", booking.Id, booking.CustomerEmail, booking.TourDate, selectedSuppliers.Count);
        } catch (Exception ex) {
            Log.Error(ex, "Booking {BookingId}: Failed to update in database.", booking.Id);
            string supportLink = GenerateUrl("UserSupport");
            ModelState.AddModelError(string.Empty, $"Unable to update your booking due to a system error. Please try again or <a href='{supportLink}'>contact support</a> if the problem persists.");
            return await CreateIndexViewWithErrorsAsync(selectedSuppliers);
        }

        return RedirectToAction("BookingSummary", new { qrToken = booking.QrToken });
    }

    [HttpGet]
    public async Task<IActionResult> BookingSummary(Guid qrToken) {
        Log.Debug("Loading booking summary page.");
        if (qrToken == Guid.Empty) {
            Log.Warning("BookingSummary called with empty QrToken.");
            return RedirectToAction("Index");
        }

        var booking = await context.Bookings.FirstOrDefaultAsync(b => b.QrToken == qrToken && !b.IsCancelled);
        if (booking == null) {
            Log.Warning("BookingSummary could not find booking with token {QrToken}.", qrToken);
            return RedirectToAction("Index");
        }

        var selectedSuppliers = await context.Suppliers
            .Where(s => booking.SelectedSupplierIds.Contains(s.Id))
            .ToListAsync();

        var bookingSummary = new BookingSummaryVM {
            TourDate = booking.TourDate,
            CustomerEmail = booking.CustomerEmail,
            PurchaseCode = booking.PurchaseCode,
            SelectedSupplierIds = booking.SelectedSupplierIds,
            SelectedSuppliers = selectedSuppliers,
            QrToken = booking.QrToken
        };

        return View(bookingSummary);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmBookingSummary(Guid qrToken) {
        if (qrToken == Guid.Empty) {
            Log.Warning("ConfirmBookingSummary called with empty QrToken.");
            return RedirectToAction("Index");
        }

        var booking = await context.Bookings.FirstOrDefaultAsync(b => b.QrToken == qrToken && !b.IsCancelled);
        if (booking == null) {
            Log.Warning("ConfirmBookingSummary could not find booking with token {QrToken}.", qrToken);
            return RedirectToAction("Index");
        }

        if (booking.IsConfirmed) {
            Log.Information("Booking {BookingId}: Already confirmed, redirecting to confirmation page.", booking.Id);
            return RedirectToAction("BookingConfirmation", new { qrToken });
        }

        try {
            booking.IsConfirmed = true;
            await context.SaveChangesAsync();

            Log.Information("Booking {BookingId}: Confirmed for {CustomerEmail} on {TourDate:dd/MM/yyyy}.", booking.Id, booking.CustomerEmail, booking.TourDate);
        } catch (Exception ex) {
            Log.Error(ex, "Booking {BookingId}: Failed to mark as confirmed.", booking.Id);
            string supportLink = GenerateUrl("UserSupport");
            return await CreateBookingSummaryErrorViewAsync(booking, $"Unable to confirm your booking due to a system error. Please try again or <a href='{supportLink}'>contact support</a> if the problem persists.");
        }

        // Non-critical operation: Send confirmation email
        bool emailFailed = false;
        try {
            await SendBookingConfirmationEmailAsync(booking);
        } catch (Exception emailEx) {
            Log.Error(emailEx, "Booking {BookingId}: confirmed but failed to send confirmation email to {CustomerEmail}.", booking.Id, booking.CustomerEmail);
            emailFailed = true;
        }

        return RedirectToAction("BookingConfirmation", new { qrToken, emailWarning = emailFailed });
    }

    [HttpGet]
    public async Task<IActionResult> BookingConfirmation(Guid qrToken, bool emailWarning = false) {
        if (qrToken == Guid.Empty) {
            Log.Warning("BookingConfirmation called with empty QrToken.");
            return RedirectToAction("Error");
        }

        var booking = await context.Bookings.FirstOrDefaultAsync(b => b.QrToken == qrToken && !b.IsCancelled);
        if (booking == null) {
            Log.Warning("BookingConfirmation could not find booking with token {QrToken}.", qrToken);
            return RedirectToAction("Error");
        }

        string? emailWarningMessage = null;
        if (emailWarning) {
            string supportUrl = GenerateUrl("UserSupport");
            emailWarningMessage = $"Your booking was saved, but we were unable to send a confirmation email. Please save the link above with your tour details and <a href='{supportUrl}'>contact support</a> for assistance.";
        }

        var model = new BookingConfirmationVM() {
            CustomerEmail = booking.CustomerEmail,
            QrToken = booking.QrToken,
            EmailWarning = emailWarningMessage
        };

        return View(model);
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
            Log.Error("Booking or BookingSuppliers is null for token {QrToken}.", qrToken);
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

        Log.Debug("Booking {BookingId}: Found {ClaimedCount} claimed suppliers and {UnclaimedCount} unclaimed suppliers.", booking.Id, claimedSupplierIds.Count, unclaimedSuppliers.Count);

        if (unclaimedSuppliers.Count == 0) {
            Log.Information("Booking {BookingId}: All suppliers already visited, redirecting to tour completed.", booking.Id);
            return RedirectToAction("TourCompleted");
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

    [HttpGet]
    public async Task<IActionResult> FoodTourCode(Guid qrToken) {
        Log.Debug("Loading FoodTourCode for qrtoken {qrToken}.", qrToken);
        if (qrToken == Guid.Empty) {
            Log.Warning("FoodTourCode action called with invalid token: {QrToken}.", qrToken);
            return RedirectToAction("Error");
        }
        var booking = await context.Bookings.FirstOrDefaultAsync(b => b.QrToken == qrToken && !b.IsCancelled);
        if (booking == null) {
            Log.Warning("FoodTourCode action could not find booking with token {QrToken}.", qrToken);
            return RedirectToAction("Error");
        }
        var tourSuppliers = await context.Suppliers
            .Where(s => booking.SelectedSupplierIds.Contains(s.Id))
            .ToListAsync();

        var model = new FoodTourCodeVM {
            TourDate = booking.TourDate,
            TourSuppliers = tourSuppliers,
            QrToken = qrToken
        };

        return View(model);
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

        string url;
        try {
            url = GenerateUrl("SupplierVisit", new { qrToken });
        } catch (InvalidOperationException ex) {
            Log.Error(ex, "Failed to generate URL for QR code with token {QrToken}.", qrToken);
            return BadRequest("Unable to generate QR code due to a system error.");
        }

        byte[] qrCodeBytes = qrImageService.GenerateQrForUrl(url);

        return File(qrCodeBytes, "image/png");
    }

    public IActionResult VisitConfirmation() {
        return View();
    }

    [HttpGet]
    public IActionResult TourCompleted() {
        return View();
    }

    public IActionResult Privacy() {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpGet]
    public IActionResult UserSupport() {
        var result = new UserSupportVM {
            SupportPhoneNumberDial = supportSettings.PhoneNumberDial,
            SupportPhoneNumberDisplay = supportSettings.PhoneNumberDisplay,
            SupportEmailAddress = supportSettings.EmailAddress
        };
        return View(result);
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

    private async Task<IActionResult> CreateBookingSummaryErrorViewAsync(Booking booking, string errorMessage) {
        var selectedSuppliers = await context.Suppliers
            .Where(s => booking.SelectedSupplierIds.Contains(s.Id))
            .ToListAsync();

        var bookingSummary = new BookingSummaryVM {
            TourDate = booking.TourDate,
            CustomerEmail = booking.CustomerEmail,
            PurchaseCode = booking.PurchaseCode,
            SelectedSupplierIds = booking.SelectedSupplierIds,
            SelectedSuppliers = selectedSuppliers,
            QrToken = booking.QrToken
        };

        ModelState.AddModelError(string.Empty, errorMessage);
        return View("BookingSummary", bookingSummary);
    }

    private async Task<IActionResult> CreateIndexViewWithErrorsAsync(List<int> selectedSuppliers) {
        var model = await CreateFoodTourViewModel();
        ViewBag.SelectedSuppliers = selectedSuppliers;
        return View(model);
    }

    private static string GetInsufficientSuppliersMessage(int supplierCount) {
        string experienceText = supplierCount == 1 ? "tasting experience is" : "tasting experiences are";
        return $"Only {supplierCount} {experienceText} currently available. You need at least {MAX_SUPPLIER_SELECTIONS} to create a tour. Please check back later.";
    }

    private void MapValidationErrorsToModelState(BookingValidationResult validationResult) {
        if (!validationResult.AreSelectedSuppliersValid) {
            // Check if it's a supplier existence issue vs count issue
            bool hasSupplierExistenceError = validationResult.ErrorMessages.Any(msg => msg.Contains("do not exist or are inactive"));

            if (hasSupplierExistenceError) {
                ModelState.AddModelError(string.Empty, "One or more selected suppliers are invalid. Please refresh and try again.");
            } else {
                ModelState.AddModelError(string.Empty, $"You must select {MAX_SUPPLIER_SELECTIONS} tasting experiences for your tour.");
            }
        }
    }

    private async Task SendBookingConfirmationEmailAsync(Booking booking) {
        try {
            var supplierIds = booking.SelectedSupplierIds;
            var suppliers = await context.Suppliers
                .Where(supplier => supplierIds.Contains(supplier.Id))
                .ToListAsync();

            string foodTourCodeUrl = GenerateUrl("FoodTourCode", new { qrToken = booking.QrToken });
            string userSupportUrl = GenerateUrl("UserSupport");

            var (subject, body) = bookingEmailService.GenerateBookingConfirmationEmail(booking, suppliers, foodTourCodeUrl, userSupportUrl);
            await emailSender.SendEmailAsync(booking.CustomerEmail, subject, body);

            Log.Information("Booking {BookingId}: Sent confirmation email to {CustomerEmail}.", booking.Id, booking.CustomerEmail);
        } catch (Exception emailEx) {
            Log.Error(emailEx, "Booking {BookingId}: Failed to send confirmation email to {CustomerEmail}.", booking.Id, booking.CustomerEmail);
            throw new InvalidOperationException("Booking saved, but we were unable to send a confirmation email. Please contact support.", emailEx);
        }
    }

    private string GenerateUrl(string actionName, object? urlValues = null) {
        string? generatedUrl = Url.Action(actionName, "Home", values: urlValues, protocol: Request.Scheme);
        if (string.IsNullOrWhiteSpace(generatedUrl)) {
            throw new InvalidOperationException($"Unable to generate {actionName} URL.");
        }
        return generatedUrl;
    }

    private async Task PopulateFormFromQrTokenAsync(FoodTourVM model, Guid? qrToken) {
        if (!qrToken.HasValue || qrToken.Value == Guid.Empty) {
            return;
        }

        var booking = await context.Bookings.FirstOrDefaultAsync(b => b.QrToken == qrToken.Value && !b.IsCancelled);
        if (booking == null) {
            Log.Debug("Index: Could not find booking with QR token {QrToken} for form population.", qrToken);
            return;
        }

        ViewBag.SelectedSuppliers = booking.SelectedSupplierIds;
        ViewBag.CustomerEmail = booking.CustomerEmail;
        ViewBag.PurchaseCode = booking.PurchaseCode;

        Log.Debug("Index: Populated form from booking {BookingId} using QR token.", booking.Id);
    }
}
