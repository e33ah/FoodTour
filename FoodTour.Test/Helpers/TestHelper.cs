namespace FoodTour.Test;

public static class TestHelper {
    private static readonly IDataProtectionProvider sharedDataProtectionProvider = new EphemeralDataProtectionProvider();

    // Shared test data/constants used across unit tests
    public const int EXPECTED_MAX_SELECTIONS = 5;
    public const string VALID_EMAIL = "customer@test.com";
    public const string VALID_CODE = "VALIDCODE123";
    public const string VALID_VOUCHER = "VOUCHER123";
    public const string NEW_VOUCHER = "NEWVOUCHER";
    public const string EXPECTED_NO_SUPPLIERS_MESSAGE = "No tasting experiences are currently available for the tour. Please check back later.";
    public const string SUPPLIER_NAME_1 = "Tasty Treats";
    public const string SUPPLIER_ADDRESS_1 = "123 Food Street";
    public const string SUPPLIER_OPENING_TIMES_1 = "9am - 5pm";
    public const string ADMIN_EMAIL = "admin@test.com";
    public const string SUPPORT_URL = "http://localhost/Home/UserSupport";
    private const string DATE_TIME_FORMAT = "HH:mm dd/MM/yyyy";
    public static readonly int nextYear = DateTime.Today.AddYears(1).Year;
    public static readonly DateTime validTourDate = new(nextYear, 6, 16, 11, 15, 0);
    public static readonly string formattedTourDate = validTourDate.ToString(DATE_TIME_FORMAT);

    // FareHarbor API-specific constants
    public const string WEYMOUTH_TOUR_NAME = "Weymouth Self-Guided Food Tour (Five Tastings)";
    public const int DEFAULT_GUEST_COUNT = 2;
    public const string DEFAULT_CUSTOMER_NAME = "Test Customer";
    public const string UPDATED_CUSTOMER_NAME = "Updated Name";
    public const string VALID_KEY = "5N8GS1ZZKC8J";
    public const string INVALID_KEY = "WRONG_KEY";

    // Alternative test emails for update/cancel scenarios
    public const string UPDATED_EMAIL = "updated@test.com";
    public const string ALTERNATIVE_EMAIL = "alternative@test.com";

    // Purchase code prefixes for different test scenarios
    public const string UPDATE_TEST_CODE = "UPDATE_TEST_001";
    public const string CANCEL_TEST_CODE = "CANCEL_TEST_001";
    public const string NO_CHANGE_TEST_CODE = "NO_CHANGE_TEST";
    public const string VALIDATION_FAIL_TEST_CODE = "VALIDATION_FAIL_TEST";
    public const string DB_FAIL_TEST_CODE = "DB_FAIL_TEST";

    // Invalid data for validation tests
    public const string INVALID_EMAIL = "invalid-email";
    public const int INVALID_GUEST_COUNT = -1;

    // Past date for validation tests
    public static readonly DateTime pastTourDate = new(2020, 1, 1, 10, 0, 0);
    public static IOptions<SupportSettings> CreateSupportOptions() {
        return Options.Create(new SupportSettings {
            PhoneNumberDisplay = "01234 567890",
            PhoneNumberDial = "+441234567890",
            EmailAddress = ADMIN_EMAIL
        });
    }

    public static AdminController CreateAdminController(IAdminAccessService? adminAccessService = null, IConfiguration? configuration = null, FoodTourContext? context = null) {
        adminAccessService ??= CreateAllowAllAdminAccessService();
        configuration ??= CreateAdminConfiguration(googleConfigured: true);
        context ??= CreateInMemoryContext();
        return new AdminController(adminAccessService, configuration, context);
    }

    public static IConfiguration CreateAdminConfiguration(bool googleConfigured = true) {
        var values = new Dictionary<string, string?>();
        if (googleConfigured) {
            values["Authentication:Google:ClientId"] = "test-client-id";
            values["Authentication:Google:ClientSecret"] = "test-client-secret";
        }
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    public static IAdminAccessService CreateAllowAllAdminAccessService() {
        return Mock.Of<IAdminAccessService>(s => s.IsAllowed(It.IsAny<string>()) == true);
    }

    public static IOptions<AdminSettings> CreateAdminSettingsOptions(params string[] allowedEmails) {
        return Options.Create(new AdminSettings {
            AllowedEmails = [.. allowedEmails]
        });
    }

    public static IOptions<AdminNotificationSettings> CreateAdminNotificationOptions() {
        return Options.Create(new AdminNotificationSettings {
            AdminEmail = ADMIN_EMAIL,
            NotifyOnVisitRecorded = true
        });
    }

    public static IDataProtectionProvider CreateDataProtectionProvider() {
        return sharedDataProtectionProvider;
    }

    public static PartnerBookingConfirmationService CreatePartnerBookingConfirmationService(FoodTourContext context) {
        return new PartnerBookingConfirmationService(context, CreateDataProtectionProvider());
    }

    public static FoodTourContext CreateInMemoryContext() {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.OpenAsync();

        var options = new DbContextOptionsBuilder<FoodTourContext>()
            .UseSqlite(connection)
            .Options;

        var context = new FoodTourContext(options);
        context.Database.EnsureCreated();

        return context;
    }

    public static Supplier CreateSupplier(int id, string? name = null, bool isActive = true, string? address = null, string? foodTourOfferDescription = null) {
        string supplierName = name ?? $"{SUPPLIER_NAME_1} {id}";
        string supplierAddress = address ?? $"{SUPPLIER_ADDRESS_1}";
        return new Supplier {
            Id = id,
            Name = supplierName,
            ContactName = "Mrs Test",
            ContactEmail = $"contact{id}@test.com",
            OpeningTimes = SUPPLIER_OPENING_TIMES_1,
            IsActive = isActive,
            Address = supplierAddress,
            FoodTourOfferDescription = foodTourOfferDescription,
            ConfirmationCode = $"CODE{id}"
        };
    }

    public static FoodTourContext CreateContextWithSuppliers(params Supplier[] suppliers) {
        var context = CreateInMemoryContext();
        context.Suppliers.AddRange(suppliers);
        context.SaveChanges();
        return context;
    }

    public static FoodTourContext CreateContextWithSuppliersAndBookings(Supplier[] suppliers, Booking[] bookings) {
        var context = CreateInMemoryContext();
        context.Suppliers.AddRange(suppliers);
        context.Bookings.AddRange(bookings);
        context.SaveChanges();
        return context;
    }

    public static FoodTourContext CreateContextWithActiveSuppliersCount(int count) {
        var suppliers = Enumerable.Range(1, count)
            .Select(i => CreateSupplier(i, $"Supplier {i}"))
            .ToArray();
        return CreateContextWithSuppliers(suppliers);
    }

    public static List<int> CreateSupplierIdList(int count) {
        return [.. Enumerable.Range(1, count)];
    }

    public static BookingSummaryVM CreateValidBookingSummaryVM(List<int> selectedSupplierIds) {
        return new BookingSummaryVM {
            TourDate = validTourDate,
            CustomerEmail = VALID_EMAIL,
            PurchaseCode = VALID_CODE,
            SelectedSupplierIds = selectedSupplierIds,
            SelectedSuppliers = [.. selectedSupplierIds.Select(id => CreateSupplier(id, $"Supplier {id}"))],
            QrToken = Guid.NewGuid()
        };
    }

    public static FoodTourContext CreateContextWithSuppliersFailingOnSave() {
        var options = new DbContextOptionsBuilder<FoodTourContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new FailingContext(options);
        var suppliers = Enumerable.Range(1, 5)
            .Select(i => CreateSupplier(i, $"Supplier {i}"))
            .ToArray();
        context.Suppliers.AddRange(suppliers);
        context.SaveChanges();
        return context;
    }

    public static FoodTourContext CreateContextFailingOnSave() {
        var options = new DbContextOptionsBuilder<FoodTourContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new FailingContext(options);
    }

    private class FailingContext(DbContextOptions<FoodTourContext> options) : FoodTourContext(options) {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
            throw new Exception("Simulated database failure during SaveChangesAsync");
        }
    }

    public static Booking CreateValidBooking(List<int>? selectedSupplierIds = null) {
        selectedSupplierIds ??= [1, 2, 3, 4, 5];
        return new Booking {
            TourDate = validTourDate,
            CustomerEmail = VALID_EMAIL,
            PurchaseCode = VALID_CODE,
            SelectedSupplierIds = selectedSupplierIds,
            BookingSuppliers = [.. selectedSupplierIds.Select(id => new BookingSupplier { SupplierId = id })],
            TotalGuests = 2
        };
    }

    public static Visit CreateVisit(int bookingId = 1, int supplierId = 1, bool isClaimed = false, bool isReimbursed = false) {
        return new Visit {
            BookingId = bookingId,
            SupplierId = supplierId,
            IsClaimed = isClaimed,
            ClaimedAt = isClaimed ? validTourDate : null,
            IsSupplierReimbursed = isReimbursed,
            ReimbursedAt = isReimbursed ? validTourDate : null
        };
    }

    /// <summary>Creates a context with a single supplier and booking, with the specified visits.</summary>
    public static async Task<(FoodTourContext context, Supplier supplier, Booking booking)> CreateContextWithVisitsAsync(Visit[] visits, CancellationToken cancellationToken = default) {
        var context = CreateInMemoryContext();
        var supplier = CreateSupplier(1);
        var booking = CreateValidBooking([supplier.Id]);
        context.Suppliers.Add(supplier);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(cancellationToken);

        foreach (var visit in visits) {
            visit.BookingId = booking.Id;
            visit.SupplierId = supplier.Id;
            context.Visits.Add(visit);
        }
        await context.SaveChangesAsync(cancellationToken);

        return (context, supplier, booking);
    }

    /// <summary>Creates a context with multiple suppliers and bookings for sorting/pagination tests.</summary>
    public static async Task<FoodTourContext> CreateContextWithMultipleVisitsAsync(
        (Supplier supplier, Booking booking, Visit visit)[] data,
        CancellationToken cancellationToken) {
        var context = CreateInMemoryContext();
        foreach (var (supplier, booking, _) in data) {
            context.Suppliers.Add(supplier);
            context.Bookings.Add(booking);
        }
        await context.SaveChangesAsync(cancellationToken);

        foreach (var (supplier, booking, visit) in data) {
            visit.BookingId = booking.Id;
            visit.SupplierId = supplier.Id;
            context.Visits.Add(visit);
        }
        await context.SaveChangesAsync(cancellationToken);

        return context;
    }

    public static Booking CreateBookingWithVisits(FoodTourContext context, int supplierId) {
        var booking = CreateValidBooking([supplierId]);
        context.Bookings.Add(booking);
        context.SaveChanges();

        // Now add the visit with the correct BookingId
        var visit = new Visit { BookingId = booking.Id, SupplierId = supplierId, IsClaimed = false };
        context.Visits.Add(visit);
        context.SaveChanges();

        return booking;
    }

    public static FoodTourApiController CreateFoodTourApiController() {
        var options = new DbContextOptionsBuilder<FoodTourContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new FoodTourContext(options);
        var bookingValidationService = new BookingValidationService(context);
        var purchaseCodeValidationService = new PurchaseCodeValidationService(context);
        return new FoodTourApiController(context, bookingValidationService, purchaseCodeValidationService);
    }

    public static FoodTourApiController CreateFoodTourApiController(FoodTourContext context) {
        var bookingValidationService = new BookingValidationService(context);
        var purchaseCodeValidationService = new PurchaseCodeValidationService(context);
        return new FoodTourApiController(context, bookingValidationService, purchaseCodeValidationService);
    }


    public static PurchaseCodeValidationService CreatePurchaseCodeValidationService(FoodTourContext? context = null) {
        context ??= CreateInMemoryContext();
        return new PurchaseCodeValidationService(context);
    }

    public static async Task<HomeController> CreateHomeController(int contextSupplierCount) {
        var context = CreateContextWithActiveSuppliersCount(contextSupplierCount);
        await context.SaveChangesAsync();
        var emailSender = Mock.Of<IEmailSender>();
        var adminNotificationService = CreateAdminNotificationService().service;
        var supplierNotificationService = CreateSupplierNotificationService().service;
        var templateRendererMock = new Mock<IRazorTemplateRenderer>();
        templateRendererMock.Setup(x => x.RenderTemplateAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync("<html>Mock email body</html>");
        var bookingEmailService = new BookingEmailService(templateRendererMock.Object);
        var supportOptions = CreateSupportOptions();
        var bookingValidationService = new BookingValidationService(context);
        var bookingUpdateService = new BookingUpdateService(context);
        var partnerBookingConfirmationService = CreatePartnerBookingConfirmationService(context);
        return new HomeController(context, emailSender, bookingEmailService, supportOptions, bookingValidationService, bookingUpdateService, partnerBookingConfirmationService, adminNotificationService, supplierNotificationService);
    }

    public static HomeController CreateHomeController(FoodTourContext context, IEmailSender? emailSender = null) {
        emailSender ??= Mock.Of<IEmailSender>();
        var adminNotificationService = CreateAdminNotificationService().service;
        var supplierNotificationService = CreateSupplierNotificationService().service;
        var templateRendererMock = new Mock<IRazorTemplateRenderer>();
        templateRendererMock.Setup(x => x.RenderTemplateAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync("<html>Mock email body</html>");
        var bookingEmailService = new BookingEmailService(templateRendererMock.Object);
        var supportOptions = CreateSupportOptions();
        var bookingValidationService = new BookingValidationService(context);
        var bookingUpdateService = new BookingUpdateService(context);
        var partnerBookingConfirmationService = CreatePartnerBookingConfirmationService(context);
        return new HomeController(context, emailSender, bookingEmailService, supportOptions, bookingValidationService, bookingUpdateService, partnerBookingConfirmationService, adminNotificationService, supplierNotificationService);
    }

    public static Mock<IEmailSender> CreateMockEmailSender() {
        var emailSenderMock = new Mock<IEmailSender>();
        emailSenderMock
            .Setup(sender => sender.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return emailSenderMock;
    }

    public static (HomeController controller, Mock<IEmailSender> emailSenderMock) CreateHomeControllerWithEmailSenderMock(FoodTourContext context) {
        var emailSenderMock = CreateMockEmailSender();
        var controller = CreateHomeController(context, emailSenderMock.Object);
        return (controller, emailSenderMock);
    }

    public static async Task<Booking> CreateBookingInDatabase(FoodTourContext context, DateTime? tourDate = null, string? email = null, string? purchaseCode = null, List<int>? selectedSupplierIds = null, int tastingCount = EXPECTED_MAX_SELECTIONS) {
        tourDate ??= validTourDate;
        email ??= VALID_EMAIL;
        purchaseCode ??= VALID_CODE;
        selectedSupplierIds ??= [1, 2, 3, 4, 5];

        var booking = new Booking {
            TourDate = tourDate.Value,
            CustomerEmail = email,
            PurchaseCode = purchaseCode,
            SelectedSupplierIds = selectedSupplierIds,
            BookingSuppliers = [.. selectedSupplierIds.Select(id => new BookingSupplier { SupplierId = id })],
            TastingCount = tastingCount
        };

        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        return booking;
    }

    public static Mock<IRazorTemplateRenderer> CreateMockTemplateRenderer() {
        var templateRendererMock = new Mock<IRazorTemplateRenderer>();
        templateRendererMock.Setup(x => x.RenderTemplateAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync("<html>Mock email body</html>");
        return templateRendererMock;
    }

    public static (SupplierNotificationService service, Mock<IEmailSender> emailSenderMock, Mock<IRazorTemplateRenderer> templateRendererMock) CreateSupplierNotificationService() {
        var emailSenderMock = new Mock<IEmailSender>();
        var templateRendererMock = CreateMockTemplateRenderer();
        var service = new SupplierNotificationService(emailSenderMock.Object, templateRendererMock.Object);
        return (service, emailSenderMock, templateRendererMock);
    }

    public static (AdminNotificationService service, Mock<IEmailSender> emailSenderMock, Mock<IRazorTemplateRenderer> templateRendererMock) CreateAdminNotificationService(bool notifyOnVisitRecorded = true, string? adminEmail = null) {
        var emailSenderMock = new Mock<IEmailSender>();
        var templateRendererMock = CreateMockTemplateRenderer();
        var options = Options.Create(new AdminNotificationSettings {
            AdminEmail = adminEmail ?? ADMIN_EMAIL,
            NotifyOnVisitRecorded = notifyOnVisitRecorded
        });
        var service = new AdminNotificationService(emailSenderMock.Object, options, templateRendererMock.Object);
        return (service, emailSenderMock, templateRendererMock);
    }
}
