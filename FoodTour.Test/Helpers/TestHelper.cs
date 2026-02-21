namespace FoodTour.Test;

public static class TestHelper {
    // Shared test data/constants used across unit tests
    public const int EXPECTED_MAX_SELECTIONS = 5;
    public const string VALID_EMAIL = "customer@test.com";
    public const string VALID_CODE = "VALIDCODE123";
    public const string NO_SUPPLIERS_MESSAGE_KEY = "NoSuppliersMessage";
    public const string INSUFFICIENT_SUPPLIERS_WARNING_KEY = "InsufficientSuppliersWarning";
    public const string EXPECTED_NO_SUPPLIERS_MESSAGE = "No tasting experiences are currently available for the tour. Please check back later.";
    public const string SUPPLIER_NAME_1 = "Tasty Treats";
    public const string SUPPLIER_ADDRESS_1 = "123 Food Street";
    public static readonly int nextYear = DateTime.Today.AddYears(1).Year;
    public static readonly DateTime validTourDate = new(nextYear, 6, 16);
    public static readonly EmailSettings testEmailSettings = new() {
        Host = "smtp.test",
        Port = 25,
        UseSsl = false,
        FromEmail = "dev@localhost",
        FromName = "Test Sender"
    };

    public static IOptions<SupportSettings> CreateSupportOptions() {
        return Options.Create(new SupportSettings {
            PhoneNumberDisplay = "01234 567890",
            PhoneNumberDial = "+441234567890",
            EmailAddress = "admin@foodtour.com"
        });
    }

    public static FoodTourContext CreateInMemoryContext() {
        var options = new DbContextOptionsBuilder<FoodTourContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new FoodTourContext(options);
    }

    public static Supplier CreateSupplier(int id, string? name = null, bool isActive = true, string? address = null) {
        string supplierName = name ?? $"{SUPPLIER_NAME_1} {id}";
        string supplierAddress = address ?? $"{SUPPLIER_ADDRESS_1}";
        return new Supplier {
            Id = id,
            Name = supplierName,
            ContactName = "Mrs Test",
            ContactEmail = $"contact{id}@test.com",
            IsActive = isActive,
            Address = supplierAddress,
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
            SelectedSuppliers = [.. selectedSupplierIds.Select(id => CreateSupplier(id, $"Supplier {id}"))]
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

    public static Booking CreateValidBooking(List<int> selectedSupplierIds) {
        return new Booking {
            TourDate = validTourDate,
            CustomerEmail = VALID_EMAIL,
            PurchaseCode = VALID_CODE,
            SelectedSupplierIds = selectedSupplierIds,
            BookingSuppliers = [.. selectedSupplierIds.Select(id => new BookingSupplier { SupplierId = id })]
        };
    }
}
