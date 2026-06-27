namespace FoodTour.Controllers;

public class HomeController(
    FoodTourContext context,
    IEmailSender emailSender,
    BookingEmailService bookingEmailService,
    IOptions<SupportSettings> supportOptions,
    IBookingValidationService bookingValidationService,
    IBookingUpdateService bookingUpdateService,
    PartnerBookingConfirmationService partnerBookingConfirmationService,
    AdminNotificationService adminNotificationService,
    SupplierNotificationService supplierNotificationService) : Controller {
    private const int MAX_SUPPLIER_SELECTIONS = 5;
    private const string EXPIRED_LINK_MESSAGE = "This link has expired, please request a new one.";
    private const string INVALID_LINK_MESSAGE = "This confirmation link is invalid. Please request a new one.";
    private readonly QrImageService qrImageService = new();
    private readonly SupportSettings supportSettings = supportOptions.Value;

    public async Task<IActionResult> Index(Guid? qrToken) {
        var model = await CreateFoodTourViewModel();

        if (model.Suppliers.Count == 0) {
            model.NoSuppliersMessage = "No tasting experiences are currently available for the tour. Please check back later.";
        } else if (model.Suppliers.Count < MAX_SUPPLIER_SELECTIONS) {
            model.InsufficientSuppliersWarning = GetInsufficientSuppliersMessage(model.Suppliers.Count);
            Log.Warning("Insufficient active suppliers: {SupplierCount} found, {RequiredCount} required.", model.Suppliers.Count, MAX_SUPPLIER_SELECTIONS);
        }

        await PopulateFormFromQrTokenAsync(qrToken, model);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(int bookingId, List<int> selectedSuppliers) {
        var booking = await TryGetBookingAsync(bookingId);
        if (booking == null) {
            return await CreateIndexViewWithErrorsAsync(selectedSuppliers);
        }

        if (booking.IsCancelled || booking.IsConfirmed) {
            return await CreateExistingBookingResultAsync(booking);
        }

        if (!await ValidateSupplierSelectionsAsync(booking, selectedSuppliers)) {
            return await CreateIndexViewWithErrorsAsync(selectedSuppliers, booking.TourDate);
        }

        if (!await ValidateSupplierExistenceAsync(bookingId, selectedSuppliers)) {
            return await CreateIndexViewWithErrorsAsync(selectedSuppliers, booking.TourDate);
        }

        if (!await TryUpdateBookingAsync(booking, selectedSuppliers)) {
            return await CreateIndexViewWithErrorsAsync(selectedSuppliers, booking.TourDate);
        }

        return RedirectToAction("BookingSummary", new { qrToken = booking.QrToken });
    }

    [HttpGet]
    public async Task<IActionResult> BookingSummary(Guid qrToken) {
        Log.Debug("Loading booking summary page.");
        var booking = await TryGetValidBookingByTokenAsync(qrToken, nameof(BookingSummary));
        if (booking == null) {
            return RedirectToAction("Index");
        }

        var selectedSuppliers = await GetSelectedSuppliersAsync(booking);

        var bookingSummary = new BookingSummaryVM {
            TourDate = booking.TourDate,
            CustomerEmail = booking.CustomerEmail,
            CustomerName = booking.CustomerName,
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
        var booking = await TryGetValidBookingByTokenAsync(qrToken, nameof(ConfirmBookingSummary));
        if (booking == null) {
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
        var booking = await TryGetValidBookingByTokenAsync(qrToken, nameof(BookingConfirmation));
        if (booking == null) {
            return RedirectToAction("Error");
        }

        string? emailWarningMessage = null;
        if (emailWarning) {
            string supportUrl = GenerateUrl("UserSupport");
            emailWarningMessage = $"Your booking was saved, but we were unable to send a confirmation email. Please save the link above with your tour details and <a href='{supportUrl}'>contact support</a> for assistance.";
        }

        var model = new BookingConfirmationVM() {
            CustomerEmail = booking.CustomerEmail,
            CustomerName = booking.CustomerName,
            QrToken = booking.QrToken,
            EmailWarning = emailWarningMessage
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> SupplierVisit(Guid qrToken) {
        var booking = await TryGetValidBookingByTokenAsync(qrToken, nameof(SupplierVisit), includeBookingSuppliers: true);
        if (booking == null || booking.BookingSuppliers == null) {
            Log.Error("Booking or BookingSuppliers is null for token {QrToken}.", qrToken);
            return RedirectToAction("Error");
        }
        if (booking.IsCancelled) {
            Log.Information("Booking {BookingId}: Booking marked as cancelled, redirecting to tour cancelled.", booking.Id);
            return RedirectToAction("TourCancelled");
        }

        var unclaimedSuppliers = await GetUnclaimedSuppliersAsync(booking);
        if (unclaimedSuppliers.Count == 0) {
            Log.Information("Booking {BookingId}: All suppliers already visited, redirecting to tour completed.", booking.Id);
            return RedirectToAction("TourCompleted");
        }

        var model = CreateSupplierVisitViewModel(booking, unclaimedSuppliers);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SupplierVisit(Guid qrToken, int selectedSupplierId, string? supplierCode, int tastingExperiencesClaimed) {
        var booking = await TryGetValidBookingByTokenAsync(qrToken, nameof(SupplierVisit), includeBookingSuppliers: true);
        if (booking == null || booking.BookingSuppliers == null) {
            Log.Error("Booking or BookingSuppliers is null for token {QrToken}.", qrToken);
            return RedirectToAction(nameof(Error));
        }

        if (booking.IsCancelled) {
            Log.Information("Booking {BookingId}: Booking marked as cancelled, redirecting to tour cancelled.", booking.Id);
            return RedirectToAction(nameof(TourCancelled));
        }

        var unclaimedSuppliers = await GetUnclaimedSuppliersAsync(booking);
        if (unclaimedSuppliers.Count == 0) {
            Log.Information("Booking {BookingId}: All suppliers already visited, redirecting to tour completed.", booking.Id);
            return RedirectToAction(nameof(TourCompleted));
        }

        if (booking.TourDate.Date != DateTime.Today) {
            Log.Warning("Booking {BookingId}: Supplier visit confirmation attempted on non-tour date.", booking.Id);
            return RedirectToAction(nameof(SupplierVisit), new { qrToken });
        }

        var supplier = unclaimedSuppliers.SingleOrDefault(s => s.Id == selectedSupplierId && s.IsActive);
        if (supplier == null) {
            ModelState.AddModelError(string.Empty, "Please select a valid business to confirm the visit.");
            return View(CreateSupplierVisitViewModel(booking, unclaimedSuppliers, selectedSupplierId, supplierCode));
        }

        if (tastingExperiencesClaimed <= 0) {
            ModelState.AddModelError(string.Empty, "Number of tasting experiences must be at least 1.");
            return View(CreateSupplierVisitViewModel(booking, unclaimedSuppliers, selectedSupplierId, supplierCode));
        }

        if (tastingExperiencesClaimed > booking.TotalGuests) {
            ModelState.AddModelError(string.Empty, $"Cannot claim {tastingExperiencesClaimed} tasting experiences. This booking has only {booking.TotalGuests} guest(s).");
            return View(CreateSupplierVisitViewModel(booking, unclaimedSuppliers, selectedSupplierId, supplierCode));
        }

        if (supplier.RequiresConfirmationCode && !string.Equals(supplier.ConfirmationCode, supplierCode, StringComparison.OrdinalIgnoreCase)) {
            ModelState.AddModelError(string.Empty, "Invalid business code provided. Please try again or contact Support.");
            return View(CreateSupplierVisitViewModel(booking, unclaimedSuppliers, selectedSupplierId, supplierCode));
        }

        var visit = await context.Visits
            .SingleOrDefaultAsync(v => v.BookingId == booking.Id && v.SupplierId == selectedSupplierId);

        if (visit == null) {
            Log.Warning("SupplierVisit POST could not find visit for booking id {BookingId} and supplier id {SupplierId}.", booking.Id, selectedSupplierId);
            ModelState.AddModelError(string.Empty, "Visit not found for this booking and business. Please contact Support.");
            return View(CreateSupplierVisitViewModel(booking, unclaimedSuppliers, selectedSupplierId, supplierCode));
        }

        try {
            int rows = await context.Visits
                .Where(v => v.Id == visit.Id && !v.IsClaimed)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(v => v.IsClaimed, true)
                    .SetProperty(v => v.ClaimedAt, v => DateTime.UtcNow)
                    .SetProperty(v => v.TastingExperiencesClaimed, tastingExperiencesClaimed));

            if (rows == 0) {
                Log.Warning("Booking {BookingId}, Supplier {SupplierId}: Visit already claimed.", booking.Id, selectedSupplierId);
                ModelState.AddModelError(string.Empty, "This visit has already been confirmed.");
                return View(CreateSupplierVisitViewModel(booking, unclaimedSuppliers, selectedSupplierId, supplierCode));
            }

            await context.Entry(visit).ReloadAsync();
        } catch (Exception ex) {
            Log.Error(ex, "Visit {VisitId}, Booking {BookingId}, Supplier {SupplierId}: Failed to save visit record.", visit.Id, booking.Id, selectedSupplierId);
            ModelState.AddModelError(string.Empty, "Unable to record visit due to a system error.");
            return View(CreateSupplierVisitViewModel(booking, unclaimedSuppliers, selectedSupplierId, supplierCode));
        }

        await TrySendVisitRecordedNotificationsAsync(booking, supplier, visit);
        return RedirectToAction(nameof(VisitConfirmation), new { claimed = tastingExperiencesClaimed, total = booking.TotalGuests });
    }

    [HttpGet]
    public async Task<IActionResult> FoodTourCode(Guid qrToken) {
        Log.Debug("Loading FoodTourCode for qrtoken {qrToken}.", qrToken);

        if (qrToken == Guid.Empty) {
            Log.Warning("FoodTourCode called with empty QrToken.");
            return RedirectToAction("Error");
        }

        var booking = await context.Bookings
            .FirstOrDefaultAsync(b => b.QrToken == qrToken);

        if (booking == null) {
            Log.Warning("FoodTourCode could not find booking with token {QrToken}.", qrToken);
            return RedirectToAction("Error");
        }

        if (booking.IsCancelled) {
            Log.Information("FoodTourCode: Booking {BookingId} is cancelled, redirecting to TourCancelled page.", booking.Id);
            return RedirectToAction("TourCancelled", new {
                customerEmail = booking.CustomerEmail,
                tourDate = booking.TourDate,
                purchaseCode = booking.PurchaseCode
            });
        }

        var tourSuppliers = await GetSelectedSuppliersAsync(booking);
        bool tourIsInPast = booking.TourDate.Date < DateTime.Today;
        bool allVisitsClaimed = tourIsInPast || await AreAllVisitsClaimedAsync(booking.Id);
        if (tourIsInPast) {
            Log.Information("FoodTourCode: Booking {BookingId} tour date {TourDate:dd/MM/yyyy} is in the past, showing completed state on QR page.", booking.Id, booking.TourDate);
        }
        var model = CreateFoodTourCodeViewModel(booking, tourSuppliers, allVisitsClaimed);

        return View(model);
    }

    [HttpGet("[controller]/qr")]
    public async Task<IActionResult> QrImage(Guid qrToken) {
        var booking = await TryGetValidBookingByTokenAsync(qrToken, nameof(QrImage));
        if (booking == null) {
            return qrToken == Guid.Empty ? BadRequest("QR Token is required.") : NotFound("Booking not found.");
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

    public IActionResult VisitConfirmation(int claimed, int total) {
        if (claimed <= 0 || total <= 0 || claimed > total) {
            Log.Warning("VisitConfirmation called with invalid parameters: claimed={Claimed}, total={Total}", claimed, total);
            return RedirectToAction("Error");
        }

        var model = new VisitConfirmationVM {
            TastingExperiencesClaimed = claimed,
            TotalGuests = total
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult TourCompleted() {
        return View();
    }

    [HttpGet]
    public IActionResult TourCancelled(string? customerEmail = null, DateTime? tourDate = null, string? purchaseCode = null) {
        var model = new TourCancelledVM {
            CustomerEmail = customerEmail ?? string.Empty,
            TourDate = tourDate ?? DateTime.MinValue,
            PurchaseCode = purchaseCode ?? string.Empty
        };
        return View(model);
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

    [HttpGet]
    public IActionResult PartnerBookingConfirmation(string bookingReference = "", string customerEmail = "", string? linkError = null) {
        var result = new PartnerBookingConfirmationVM {
            BookingReference = bookingReference,
            CustomerEmail = customerEmail,
            LinkErrorMessage = GetPartnerBookingLinkErrorMessage(linkError)
        };

        return View(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PartnerBookingConfirmation(PartnerBookingConfirmationVM model) {
        if (!ModelState.IsValid) {
            return View(model);
        }

        string bookingReference = model.BookingReference.Trim();
        string customerEmail = model.CustomerEmail.Trim();

        var booking = await GetPendingPartnerBookingAsync(bookingReference);
        if (booking == null) {
            ModelState.AddModelError(string.Empty, "We could not find a partner booking awaiting email confirmation for that reference.");
            return View(model);
        }

        try {
            string validation = partnerBookingConfirmationService.CreateValidationToken(booking.Id, customerEmail, bookingReference);
            string validationUrl = GenerateUrl("CompletePartnerBookingConfirmation", new { validation });
            string privacyPolicyUrl = GenerateUrl("Privacy");

            var (subject, body) = await bookingEmailService.GeneratePartnerBookingConfirmationEmailAsync(customerEmail, bookingReference, validationUrl, privacyPolicyUrl);
            await emailSender.SendEmailAsync(customerEmail, subject, body);
        } catch (Exception ex) {
            Log.Error(ex, "Booking {BookingId}: Failed to send partner booking confirmation email to {CustomerEmail}.", booking.Id, customerEmail);
            ModelState.AddModelError(string.Empty, "We were unable to send your confirmation email. Please try again or contact support if the problem persists.");
            return View(model);
        }

        model.CustomerEmail = customerEmail;
        model.BookingReference = bookingReference;
        model.EmailSent = true;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CompletePartnerBookingConfirmation(string validation) {
        var result = await partnerBookingConfirmationService.TryConfirmAsync(validation);
        if (result.Status != PartnerBookingConfirmationService.PartnerBookingConfirmationStatus.Success || result.Booking == null) {
            return RedirectToAction(nameof(PartnerBookingConfirmation), new {
                bookingReference = result.BookingReference ?? string.Empty,
                customerEmail = result.CustomerEmail ?? string.Empty,
                linkError = result.Status == PartnerBookingConfirmationService.PartnerBookingConfirmationStatus.Expired ? "expired" : "invalid"
            });
        }

        return RedirectToAction(nameof(Index), new {
            qrToken = result.Booking.QrToken,
            bookingReference = result.Booking.VoucherNumber ?? result.Booking.PurchaseCode
        });
    }

    private async Task<FoodTourVM> CreateFoodTourViewModel(DateTime? tourDate = null) {
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
            MaxSelections = MAX_SUPPLIER_SELECTIONS,
            TourDate = tourDate
        };
    }

    private async Task<IActionResult> CreateBookingSummaryErrorViewAsync(Booking booking, string errorMessage) {
        var selectedSuppliers = await GetSelectedSuppliersAsync(booking);

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

    private async Task<IActionResult> CreateIndexViewWithErrorsAsync(List<int> selectedSuppliers, DateTime? tourDate = null) {
        var model = await CreateFoodTourViewModel(tourDate);
        model.SelectedSuppliers = selectedSuppliers;
        if (Request.HasFormContentType) {
            model.CustomerEmail = Request.Form["customerEmail"].ToString();
            model.PurchaseCode = Request.Form["purchaseCode"].ToString();
        }
        return View("Index", model);
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
            var suppliers = await GetSelectedSuppliersAsync(booking);

            string foodTourCodeUrl = GenerateUrl("FoodTourCode", new { qrToken = booking.QrToken });
            string userSupportUrl = GenerateUrl("UserSupport");

            var (subject, body) = await bookingEmailService.GenerateBookingConfirmationEmailAsync(booking, suppliers, foodTourCodeUrl, userSupportUrl);
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

    private async Task PopulateFormFromQrTokenAsync(Guid? qrToken, FoodTourVM model) {
        if (!qrToken.HasValue || qrToken.Value == Guid.Empty) {
            return;
        }

        var booking = await TryGetValidBookingByTokenAsync(qrToken.Value, nameof(PopulateFormFromQrTokenAsync));
        if (booking == null) {
            return;
        }

        model.SelectedSuppliers = booking.SelectedSupplierIds;
        model.CustomerEmail = booking.CustomerEmail;
        model.PurchaseCode = !string.IsNullOrWhiteSpace(booking.VoucherNumber) ? booking.VoucherNumber : booking.PurchaseCode;
        model.TourDate = booking.TourDate;

        Log.Debug("Index: Populated form from booking {BookingId} using QR token.", booking.Id);
    }

    private async Task<IActionResult> CreateExistingBookingResultAsync(Booking booking) {
        if (booking.IsCancelled) {
            Log.Information("Index POST: Booking {BookingId} is cancelled, redirecting to cancelled page.", booking.Id);
            return RedirectToAction("TourCancelled", new {
                customerEmail = booking.CustomerEmail,
                tourDate = booking.TourDate,
                purchaseCode = booking.PurchaseCode
            });
        }

        if (await AreAllVisitsClaimedAsync(booking.Id)) {
            Log.Information("Index POST: Booking {BookingId} has completed all visits, redirecting to completed page.", booking.Id);
            return RedirectToAction("TourCompleted");
        }

        Log.Information("Index POST: Booking {BookingId} is already confirmed, redirecting to tour details.", booking.Id);
        return RedirectToAction("FoodTourCode", new { qrToken = booking.QrToken });
    }

    private async Task<Booking?> TryGetBookingAsync(int bookingId) {
        if (bookingId <= 0) {
            Log.Warning("Index POST called with invalid or missing booking ID: {BookingId}", bookingId);
            ModelState.AddModelError(string.Empty, "Your booking could not be validated. Please enter your email and booking reference.");
            return null;
        }

        var booking = await context.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null) {
            Log.Warning("Index POST could not find booking with ID: {BookingId}", bookingId);
            ModelState.AddModelError(string.Empty, "Your booking could not be found. Please verify your details.");
            return null;
        }

        return booking;
    }

    private async Task<Booking?> TryGetValidBookingByTokenAsync(Guid qrToken, string callingMethod, bool includeBookingSuppliers = false) {
        if (qrToken == Guid.Empty) {
            Log.Warning("{CallingMethod} called with empty QrToken.", callingMethod);
            return null;
        }

        IQueryable<Booking> query = context.Bookings;
        if (includeBookingSuppliers) {
            query = query.Include(b => b.BookingSuppliers).ThenInclude(bs => bs.Supplier);
        }

        var booking = await query.FirstOrDefaultAsync(b => b.QrToken == qrToken);
        if (booking == null) {
            Log.Warning("{CallingMethod} could not find booking with token {QrToken}.", callingMethod, qrToken);
            return null;
        }
        return booking;
    }

    private async Task<bool> ValidateSupplierSelectionsAsync(Booking booking, List<int> selectedSuppliers) {
        var validationRequest = new BookingValidationRequest {
            TourDate = booking.TourDate,
            CustomerEmail = booking.CustomerEmail,
            SelectedSupplierIds = selectedSuppliers
        };

        var validationResult = await bookingValidationService.ValidateAsync(validationRequest);

        if (!validationResult.IsValid) {
            MapValidationErrorsToModelState(validationResult);
            return false;
        }

        return true;
    }

    private async Task<bool> ValidateSupplierExistenceAsync(int bookingId, List<int> selectedSuppliers) {
        var existingSupplierIds = await context.Suppliers
            .Where(s => selectedSuppliers.Contains(s.Id) && s.IsActive)
            .Select(s => s.Id)
            .ToListAsync();

        if (existingSupplierIds.Count != selectedSuppliers.Count) {
            Log.Warning("Booking {BookingId}: One or more selected supplier IDs do not exist in the database. Provided: {ProvidedIds}, Existing: {ExistingIds}", bookingId, selectedSuppliers, existingSupplierIds);
            ModelState.AddModelError(string.Empty, "One or more selected suppliers are invalid. Please refresh and try again.");
            return false;
        }

        return true;
    }

    private async Task<bool> TryUpdateBookingAsync(Booking booking, List<int> selectedSuppliers) {
        try {
            await bookingUpdateService.UpdateBookingSupplierSelectionsAsync(booking, selectedSuppliers);
            return true;
        } catch (Exception ex) {
            Log.Error(ex, "Booking {BookingId}: Failed to update in database.", booking.Id);
            string supportLink = GenerateUrl("UserSupport");
            ModelState.AddModelError(string.Empty, $"Unable to update your booking due to a system error. Please try again or <a href='{supportLink}'>contact support</a> if the problem persists.");
            return false;
        }
    }

    private async Task<List<Supplier>> GetSelectedSuppliersAsync(Booking booking) {
        return await context.Suppliers
            .Where(s => booking.SelectedSupplierIds.Contains(s.Id))
            .ToListAsync();
    }

    private async Task<bool> AreAllVisitsClaimedAsync(int bookingId) {
        var visits = await context.Visits
            .Where(v => v.BookingId == bookingId)
            .ToListAsync();

        return visits.Count > 0 && visits.All(v => v.IsClaimed);
    }

    private async Task<List<Supplier>> GetUnclaimedSuppliersAsync(Booking booking) {
        var claimedSupplierIds = await context.Visits
            .Where(v => v.BookingId == booking.Id && v.IsClaimed)
            .Select(v => v.SupplierId)
            .ToListAsync();

        var unclaimedSuppliers = booking.BookingSuppliers!
            .Where(bs => !claimedSupplierIds.Contains(bs.SupplierId))
            .Select(bs => bs.Supplier)
            .ToList();

        Log.Debug("Booking {BookingId}: Found {ClaimedCount} claimed suppliers and {UnclaimedCount} unclaimed suppliers.",
            booking.Id, claimedSupplierIds.Count, unclaimedSuppliers.Count);

        return unclaimedSuppliers;
    }

    private static SupplierVisitVM CreateSupplierVisitViewModel(Booking booking, List<Supplier> unclaimedSuppliers, int selectedSupplierId = 0, string? supplierCode = null) {
        return new SupplierVisitVM {
            BookingId = booking.Id,
            QrToken = booking.QrToken,
            Suppliers = unclaimedSuppliers,
            TourDate = booking.TourDate,
            CustomerEmail = booking.CustomerEmail,
            TotalGuests = booking.TotalGuests,
            SelectedSupplierId = selectedSupplierId,
            SupplierCode = supplierCode ?? string.Empty
        };
    }

    private async Task TrySendVisitRecordedNotificationsAsync(Booking booking, Supplier supplier, Visit visit) {
        try {
            await adminNotificationService.NotifyVisitRecordedAsync(booking, supplier, visit);
            Log.Information("Visit ID {VisitId}: Admin notification email sent", visit.Id);
        } catch (Exception emailEx) {
            Log.Error(emailEx, "Visit ID {VisitId}: Visit recorded but failed to notify admin. BookingId {BookingId}, SupplierId {SupplierId}.", visit.Id, booking.Id, supplier.Id);
        }

        if (!supplier.EmailNotificationsEnabled) {
            return;
        }

        try {
            string supportUrl = "/Home/UserSupport";
            await supplierNotificationService.NotifySupplierVisitRecordedAsync(booking, supplier, visit, supportUrl);
            Log.Information("Visit ID {VisitId}: Supplier notification email sent to {SupplierEmail}", visit.Id, supplier.ContactEmail);
        } catch (Exception emailEx) {
            Log.Error(emailEx, "Visit ID {VisitId}: Visit recorded but failed to notify supplier. BookingId {BookingId}, SupplierId {SupplierId}.", visit.Id, booking.Id, supplier.Id);
        }
    }

    private static FoodTourCodeVM CreateFoodTourCodeViewModel(Booking booking, List<Supplier> suppliers, bool allVisitsClaimed) {
        return new FoodTourCodeVM {
            TourDate = booking.TourDate,
            TourSuppliers = suppliers,
            QrToken = booking.QrToken,
            AllVisitsClaimed = allVisitsClaimed
        };
    }

    private static string? GetPartnerBookingLinkErrorMessage(string? linkError) {
        return linkError switch {
            "expired" => EXPIRED_LINK_MESSAGE,
            "invalid" => INVALID_LINK_MESSAGE,
            _ => null
        };
    }

    private async Task<Booking?> GetPendingPartnerBookingAsync(string bookingReference) {
        if (string.IsNullOrWhiteSpace(bookingReference)) {
            return null;
        }

        return await context.Bookings
            .FirstOrDefaultAsync(b => b.VoucherNumber == bookingReference && b.RequiresEmailConfirmation && !b.IsCancelled);
    }

}
