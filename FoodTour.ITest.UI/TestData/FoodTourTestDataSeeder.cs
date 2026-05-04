namespace FoodTour.ITest.UI.TestData;

public static class FoodTourTestDataSeeder {
    private const string DEFAULT_SUPPLIER_CONFIRMATION_CODE = "TOUR";
    private const int REQUIRED_SUPPLIER_SELECTION_COUNT = 5;
    private const int UNCLAIMED_TASTING_EXPERIENCES = 0;

    private static readonly DateTime bookingSelectionFutureTourDate = CreateUtcDateTime(2035, 06, 11, 11, 15, 00);
    private static readonly DateTime confirmedBookingFutureTourDate = CreateUtcDateTime(2035, 06, 13, 12, 00, 00);
    private static readonly DateTime cancelledBookingFutureTourDate = CreateUtcDateTime(2035, 06, 14, 12, 00, 00);
    private static readonly DateTime completedPastTourDate = CreateUtcDateTime(2024, 02, 14, 19, 00, 00);
    private static readonly DateTime supplierVisitFutureTourDate = CreateUtcDateTime(2035, 06, 16, 13, 00, 00);
    private static readonly DateTime partnerConfirmationFutureTourDate = CreateUtcDateTime(2035, 06, 11, 11, 15, 00);
    private static readonly Guid sunnyBakerQrToken = Guid.Parse("3FA85F64-5717-4562-B3FC-2C963F66AFA6");
    private static readonly Guid lunaRiversQrToken = Guid.Parse("8B5C1F29-A347-4D21-9E8A-7F3B2D4E6C8A");
    private static readonly Guid mapleFrostQrToken = Guid.Parse("9A0B1C2D-3E4F-5A6B-7C8D-9E0F1A2B3C4D");
    private static readonly Guid graceValentineQrToken = Guid.Parse("4D5E6F7A-8B9C-0D1E-2F3A-4B5C6D7E8F9A");
    private static readonly Guid cloverGreeneQrToken = Guid.Parse("2F3A4B5C-6D7E-8F9A-0B1C-2D3E4F5A6B7C");
    private static readonly Guid mrsTestQrToken = Guid.Parse("6C0B8F12-6C86-4EF2-B2F8-4D6FC533B9D1");
    private static readonly BookingSeedDefinition bookingSelectionDefinition = new(
        CustomerName: "Sunny Baker",
        CustomerEmail: "sunny.baker@happymail.com",
        PurchaseCode: "123123123",
        VoucherNumber: null,
        QrToken: sunnyBakerQrToken,
        TourDate: bookingSelectionFutureTourDate,
        CreatedAt: CreateBookingCreatedAt(bookingSelectionFutureTourDate),
        TotalGuests: 4,
        IsConfirmed: false,
        IsCancelled: false,
        RequiresEmailConfirmation: false,
        CancelledAt: null);
    private static readonly BookingSeedDefinition confirmedBookingDefinition = new(
        CustomerName: "Luna Rivers",
        CustomerEmail: "luna.rivers@moonlight.net",
        PurchaseCode: "123123124",
        VoucherNumber: null,
        QrToken: lunaRiversQrToken,
        TourDate: confirmedBookingFutureTourDate,
        CreatedAt: CreateBookingCreatedAt(confirmedBookingFutureTourDate),
        TotalGuests: 1,
        IsConfirmed: true,
        IsCancelled: false,
        RequiresEmailConfirmation: false,
        CancelledAt: null);
    private static readonly BookingSeedDefinition cancelledBookingDefinition = new(
        CustomerName: "Maple Frost",
        CustomerEmail: "maple.frost@cozydays.com",
        PurchaseCode: "123123130",
        VoucherNumber: null,
        QrToken: mapleFrostQrToken,
        TourDate: cancelledBookingFutureTourDate,
        CreatedAt: CreateBookingCreatedAt(cancelledBookingFutureTourDate),
        TotalGuests: 2,
        IsConfirmed: false,
        IsCancelled: true,
        RequiresEmailConfirmation: false,
        CancelledAt: CreateCancelledAt(cancelledBookingFutureTourDate));
    private static readonly BookingSeedDefinition pastBookingDefinition = new(
        CustomerName: "Grace Valentine",
        CustomerEmail: "grace.valentine@sweetheart.net",
        PurchaseCode: "123123131",
        VoucherNumber: null,
        QrToken: graceValentineQrToken,
        TourDate: completedPastTourDate,
        CreatedAt: CreateBookingCreatedAt(completedPastTourDate),
        TotalGuests: 2,
        IsConfirmed: true,
        IsCancelled: false,
        RequiresEmailConfirmation: false,
        CancelledAt: null);
    private static readonly BookingSeedDefinition supplierVisitBookingDefinition = new(
        CustomerName: "Clover Greene",
        CustomerEmail: "clover.greene@luckycharm.com",
        PurchaseCode: "123123132",
        VoucherNumber: null,
        QrToken: cloverGreeneQrToken,
        TourDate: supplierVisitFutureTourDate,
        CreatedAt: CreateBookingCreatedAt(supplierVisitFutureTourDate),
        TotalGuests: 6,
        IsConfirmed: true,
        IsCancelled: false,
        RequiresEmailConfirmation: false,
        CancelledAt: null);
    private static readonly BookingSeedDefinition partnerConfirmationBookingDefinition = new(
        CustomerName: "Mrs Test",
        CustomerEmail: "mrstest@example.com",
        PurchaseCode: "987654321",
        VoucherNumber: "TEST123456789",
        QrToken: mrsTestQrToken,
        TourDate: partnerConfirmationFutureTourDate,
        CreatedAt: CreateBookingCreatedAt(partnerConfirmationFutureTourDate),
        TotalGuests: 2,
        IsConfirmed: false,
        IsCancelled: false,
        RequiresEmailConfirmation: true,
        CancelledAt: null);

    public static async Task<FoodTourSeedData> SeedAsync(FoodTourContext context) {
        bool hasExistingData = await context.Suppliers.AnyAsync()
            || await context.Bookings.AnyAsync()
            || await context.Visits.AnyAsync();

        if (hasExistingData) {
            throw new InvalidOperationException("The UI test database must be empty before sample data is seeded.");
        }

        var suppliers = CreateSuppliers();
        await context.Suppliers.AddRangeAsync(suppliers);
        await context.SaveChangesAsync();

        var activeSuppliers = suppliers.Where(supplier => supplier.IsActive).ToList();
        var standardSupplierIds = activeSuppliers
            .Take(REQUIRED_SUPPLIER_SELECTION_COUNT)
            .Select(supplier => supplier.Id)
            .ToList();

        var bookings = CreateBookings(standardSupplierIds, suppliers);
        await context.Bookings.AddRangeAsync(bookings);
        await context.SaveChangesAsync();

        var supplierVisitBooking = bookings.Single(booking => booking.QrToken == cloverGreeneQrToken);
        var visits = CreateVisits(supplierVisitBooking, standardSupplierIds);
        await context.Visits.AddRangeAsync(visits);
        await context.SaveChangesAsync();

        var seededSuppliers = suppliers
            .Select(supplier => new SeededSupplier(supplier.Id, supplier.Name, supplier.ConfirmationCode, supplier.IsActive))
            .ToList();
        var seededBookings = bookings
            .Select(CreateSeededBooking)
            .ToList();

        return new FoodTourSeedData {
            Suppliers = seededSuppliers,
            ActiveSuppliers = [.. seededSuppliers.Where(supplier => supplier.IsActive)],
            Bookings = seededBookings,
            RequiredSupplierSelectionCount = REQUIRED_SUPPLIER_SELECTION_COUNT,
            InactiveSupplier = seededSuppliers.Single(supplier => !supplier.IsActive),
            BookingSelectionBooking = seededBookings.Single(booking => booking.QrToken == sunnyBakerQrToken),
            ConfirmedBooking = seededBookings.Single(booking => booking.QrToken == lunaRiversQrToken),
            CancelledBooking = seededBookings.Single(booking => booking.QrToken == mapleFrostQrToken),
            PastBooking = seededBookings.Single(booking => booking.QrToken == graceValentineQrToken),
            SupplierVisitBooking = seededBookings.Single(booking => booking.QrToken == cloverGreeneQrToken),
            PartnerConfirmationBooking = seededBookings.Single(booking => booking.QrToken == mrsTestQrToken),
            SupplierVisitBookingVisits = [.. visits.Select(visit => new SeededVisit(visit.Id, visit.BookingId, visit.SupplierId, visit.IsClaimed))]
        };
    }

    private static List<Supplier> CreateSuppliers() {
        return [
            CreateSupplier(
                name: "Fish and Chips",
                address: "1 High Street, Weymouth",
                contactName: "Bob Smith",
                contactEmail: "bob@example.com",
                description: "Traditional British fish and chips served with mushy peas and tartare sauce. Fresh locally caught fish battered to perfection and fried to a golden crisp.",
                tag1: "Fish&Chips",
                tag2: "Lunch",
                tag3: "Dinner",
                openingTimes: "11am - 3pm"),
            CreateSupplier(
                name: "Cream Teas",
                address: "3B High Street, Weymouth",
                contactName: "Sue Smith",
                contactEmail: "sue@example.com",
                tag1: "AfternoonTea",
                openingTimes: string.Empty),
            CreateSupplier(
                name: "Coffee and Cake",
                address: "42 High Street, Weymouth",
                contactName: "Jo Smith",
                contactEmail: "jo@example.com",
                tag1: "Coffee",
                tag2: "Breakfast",
                openingTimes: "Monday-Friday: 8am - 5pm; Saturday: 9am - 4pm; Sunday: 9am - 3pm",
                openingTimesMonday: "8am - 5pm",
                openingTimesTuesday: "8am - 5pm",
                openingTimesWednesday: "8am - 5pm",
                openingTimesThursday: "8am - 5pm",
                openingTimesFriday: "8am - 5pm",
                openingTimesSaturday: "9am - 4pm",
                openingTimesSunday: "9am - 3pm"),
            CreateSupplier(
                name: "Pirates Pub",
                address: "1 Cliff Street, Weymouth",
                contactName: "Jack Sparrow",
                contactEmail: "jack@example.com",
                tag1: "Lunch",
                tag2: "Dinner",
                openingTimes: "2pm - late"),
            CreateSupplier(
                name: "Artisan Bakery",
                address: "15 Harbour Road, Weymouth",
                contactName: "Emma Baker",
                contactEmail: "emma@example.com",
                description: "Award-winning artisan bakery specialising in sourdough breads, handmade pastries, and traditional British bakes. All products made fresh daily using organic flour and locally sourced ingredients.",
                tag1: "Breakfast",
                tag2: "Coffee",
                openingTimes: "Monday-Friday: 9am - 5pm; Saturday: 10am - 4pm; Sunday: Closed",
                openingTimesMonday: "9am - 5pm",
                openingTimesTuesday: "9am - 5pm",
                openingTimesWednesday: "9am - 5pm",
                openingTimesThursday: "9am - 5pm",
                openingTimesFriday: "9am - 5pm",
                openingTimesSaturday: "10am - 4pm",
                openingTimesSunday: "Closed"),
            CreateSupplier(
                name: "Seafood Shack",
                address: "8 The Esplanade, Weymouth",
                contactName: "Tom Fisher",
                contactEmail: "tom@example.com",
                tag1: "Lunch",
                tag2: "Dinner",
                openingTimes: "Tuesday-Saturday: 10am - 6pm"),
            CreateSupplier(
                name: "Ice Cream Parlour",
                address: "22 Beach Street, Weymouth",
                contactName: "Lucy Jones",
                contactEmail: "lucy@example.com",
                description: "Luxury ice cream parlour offering over 30 flavours of homemade Italian gelato and ice cream. Made with natural ingredients and featuring seasonal specialities alongside classic favourites.",
                tag1: "IceCream",
                openingTimes: "10am - 4pm"),
            CreateSupplier(
                name: "Closed for Business",
                address: "1 Shut Street, Weymouth",
                contactName: "MT House",
                contactEmail: "mt@example.com",
                isActive: false)
        ];
    }

    private static List<Booking> CreateBookings(IReadOnlyList<int> standardSupplierIds, IReadOnlyList<Supplier> suppliers) {
        var standardSuppliers = suppliers
            .Where(supplier => standardSupplierIds.Contains(supplier.Id))
            .ToList();

        return [
            CreateBooking(bookingSelectionDefinition),
            CreateBooking(confirmedBookingDefinition, standardSupplierIds, standardSuppliers),
            CreateBooking(cancelledBookingDefinition),
            CreateBooking(pastBookingDefinition, standardSupplierIds, standardSuppliers),
            CreateBooking(supplierVisitBookingDefinition, standardSupplierIds, standardSuppliers),
            CreateBooking(partnerConfirmationBookingDefinition)
        ];
    }

    private static Supplier CreateSupplier(
        string name,
        string address,
        string contactName,
        string contactEmail,
        bool isActive = true,
        string? description = null,
        string? tag1 = null,
        string? tag2 = null,
        string? tag3 = null,
        string? tag4 = null,
        string? openingTimes = null,
        string? openingTimesMonday = null,
        string? openingTimesTuesday = null,
        string? openingTimesWednesday = null,
        string? openingTimesThursday = null,
        string? openingTimesFriday = null,
        string? openingTimesSaturday = null,
        string? openingTimesSunday = null) {
        return new Supplier {
            Name = name,
            Address = address,
            Description = description,
            ContactName = contactName,
            ContactEmail = contactEmail,
            IsActive = isActive,
            Tag1 = tag1,
            Tag2 = tag2,
            Tag3 = tag3,
            Tag4 = tag4,
            ConfirmationCode = DEFAULT_SUPPLIER_CONFIRMATION_CODE,
            OpeningTimes = openingTimes,
            OpeningTimesMonday = openingTimesMonday,
            OpeningTimesTuesday = openingTimesTuesday,
            OpeningTimesWednesday = openingTimesWednesday,
            OpeningTimesThursday = openingTimesThursday,
            OpeningTimesFriday = openingTimesFriday,
            OpeningTimesSaturday = openingTimesSaturday,
            OpeningTimesSunday = openingTimesSunday,
            EmailNotificationsEnabled = isActive
        };
    }

    private static Booking CreateBooking(
        BookingSeedDefinition bookingDefinition,
        IReadOnlyList<int>? selectedSupplierIds = null,
        IReadOnlyList<Supplier>? selectedSuppliers = null) {
        var bookingSupplierIds = selectedSupplierIds?.ToList() ?? [];
        var bookingSuppliers = selectedSuppliers?.Select(supplier => new BookingSupplier { Supplier = supplier }).ToList() ?? [];

        return new Booking {
            TourDate = bookingDefinition.TourDate,
            CustomerEmail = bookingDefinition.CustomerEmail,
            PurchaseCode = bookingDefinition.PurchaseCode,
            VoucherNumber = bookingDefinition.VoucherNumber,
            SelectedSupplierIds = bookingSupplierIds,
            CreatedAt = bookingDefinition.CreatedAt,
            TotalGuests = bookingDefinition.TotalGuests,
            CustomerName = bookingDefinition.CustomerName,
            QrToken = bookingDefinition.QrToken,
            IsConfirmed = bookingDefinition.IsConfirmed,
            IsCancelled = bookingDefinition.IsCancelled,
            CancelledAt = bookingDefinition.CancelledAt,
            RequiresEmailConfirmation = bookingDefinition.RequiresEmailConfirmation,
            BookingSuppliers = bookingSuppliers
        };
    }

    private static List<Visit> CreateVisits(Booking booking, IReadOnlyList<int> supplierIds) {
        return [.. supplierIds
            .Select(supplierId => new Visit {
                Booking = booking,
                SupplierId = supplierId,
                IsClaimed = false,
                TastingExperiencesClaimed = UNCLAIMED_TASTING_EXPERIENCES
            })];
    }

    private static DateTime CreateUtcDateTime(int year, int month, int day, int hour, int minute, int second) {
        return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
    }

    private static DateTime CreateBookingCreatedAt(DateTime tourDate) {
        return CreateRelatedUtcDateTime(tourDate, -7, 09, 00, 00);
    }

    private static DateTime CreateCancelledAt(DateTime tourDate) {
        return CreateRelatedUtcDateTime(tourDate, -2, 10, 00, 00);
    }

    private static DateTime CreateRelatedUtcDateTime(DateTime referenceDate, int dayOffset, int hour, int minute, int second) {
        var relatedDateTime = referenceDate.Date
            .AddDays(dayOffset)
            .AddHours(hour)
            .AddMinutes(minute)
            .AddSeconds(second);

        return DateTime.SpecifyKind(relatedDateTime, DateTimeKind.Utc);
    }

    private static SeededBooking CreateSeededBooking(Booking booking) {
        return new SeededBooking(
            booking.Id,
            booking.CustomerEmail,
            booking.PurchaseCode,
            booking.VoucherNumber,
            booking.QrToken,
            booking.TourDate,
            booking.IsConfirmed,
            booking.IsCancelled,
            booking.RequiresEmailConfirmation,
            booking.TotalGuests);
    }

    private sealed record BookingSeedDefinition(
        string CustomerName,
        string CustomerEmail,
        string PurchaseCode,
        string? VoucherNumber,
        Guid QrToken,
        DateTime TourDate,
        DateTime CreatedAt,
        int TotalGuests,
        bool IsConfirmed,
        bool IsCancelled,
        bool RequiresEmailConfirmation,
        DateTime? CancelledAt);
}
