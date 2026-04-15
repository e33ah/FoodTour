namespace FoodTour.Test.ServicesTest;

public class BookingUpdateServiceTests : TestBase {
    private readonly FoodTourContext context;
    private readonly BookingUpdateService sut;

    public BookingUpdateServiceTests() {
        var options = new DbContextOptionsBuilder<FoodTourContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        context = new FoodTourContext(options);
        sut = new BookingUpdateService(context);
    }

    [Fact]
    public async Task UpdateBookingSupplierSelectionsAsync_UpdatesBookingWithNewSuppliers_WhenCalled() {
        var booking = TestHelper.CreateValidBooking();
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);

        var supplier1 = TestHelper.CreateSupplier(1);
        var supplier2 = TestHelper.CreateSupplier(2);
        var supplier3 = TestHelper.CreateSupplier(3);
        context.Suppliers.AddRange(supplier1, supplier2, supplier3);
        await context.SaveChangesAsync(CancellationToken);

        var selectedSupplierIds = new List<int> { supplier1.Id, supplier2.Id, supplier3.Id };

        await sut.UpdateBookingSupplierSelectionsAsync(booking, selectedSupplierIds);

        var updatedBooking = await context.Bookings.FirstAsync(b => b.Id == booking.Id, CancellationToken);
        updatedBooking.SelectedSupplierIds.ShouldBe(selectedSupplierIds);
    }

    [Fact]
    public async Task UpdateBookingSupplierSelectionsAsync_CreatesBookingSuppliers_WhenCalled() {
        var booking = TestHelper.CreateValidBooking();
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);

        var supplier1 = TestHelper.CreateSupplier(1);
        var supplier2 = TestHelper.CreateSupplier(2);
        context.Suppliers.AddRange(supplier1, supplier2);
        await context.SaveChangesAsync(CancellationToken);

        var selectedSupplierIds = new List<int> { supplier1.Id, supplier2.Id };

        await sut.UpdateBookingSupplierSelectionsAsync(booking, selectedSupplierIds);

        var bookingSuppliers = await context.BookingSuppliers
            .Where(bs => bs.BookingId == booking.Id)
            .ToListAsync(CancellationToken);

        bookingSuppliers.Count.ShouldBe(2);
        bookingSuppliers.ShouldContain(bs => bs.SupplierId == supplier1.Id);
        bookingSuppliers.ShouldContain(bs => bs.SupplierId == supplier2.Id);
    }

    [Fact]
    public async Task UpdateBookingSupplierSelectionsAsync_CreatesVisits_WhenCalled() {
        var booking = TestHelper.CreateValidBooking();
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);

        var supplier1 = TestHelper.CreateSupplier(1);
        var supplier2 = TestHelper.CreateSupplier(2);
        context.Suppliers.AddRange(supplier1, supplier2);
        await context.SaveChangesAsync(CancellationToken);

        var selectedSupplierIds = new List<int> { supplier1.Id, supplier2.Id };

        await sut.UpdateBookingSupplierSelectionsAsync(booking, selectedSupplierIds);

        var visits = await context.Visits
            .Where(v => v.BookingId == booking.Id)
            .ToListAsync(CancellationToken);

        visits.Count.ShouldBe(2);
        visits.ShouldAllBe(v => !v.IsClaimed);
        visits.ShouldContain(v => v.SupplierId == supplier1.Id);
        visits.ShouldContain(v => v.SupplierId == supplier2.Id);
    }

    [Fact]
    public async Task UpdateBookingSupplierSelectionsAsync_RemovesOldBookingSuppliers_WhenUpdatingExistingBooking() {
        var booking = TestHelper.CreateValidBooking();
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);

        var supplier1 = TestHelper.CreateSupplier(1);
        var supplier2 = TestHelper.CreateSupplier(2);
        var supplier3 = TestHelper.CreateSupplier(3);
        context.Suppliers.AddRange(supplier1, supplier2, supplier3);
        await context.SaveChangesAsync(CancellationToken);

        var oldBookingSupplier1 = new BookingSupplier { BookingId = booking.Id, SupplierId = supplier1.Id };
        var oldBookingSupplier2 = new BookingSupplier { BookingId = booking.Id, SupplierId = supplier2.Id };
        context.BookingSuppliers.AddRange(oldBookingSupplier1, oldBookingSupplier2);
        await context.SaveChangesAsync(CancellationToken);

        var newSelectedSupplierIds = new List<int> { supplier3.Id };

        await sut.UpdateBookingSupplierSelectionsAsync(booking, newSelectedSupplierIds);

        var bookingSuppliers = await context.BookingSuppliers
            .Where(bs => bs.BookingId == booking.Id)
            .ToListAsync(CancellationToken);

        bookingSuppliers.Count.ShouldBe(1);
        bookingSuppliers[0].SupplierId.ShouldBe(supplier3.Id);
    }

    [Fact]
    public async Task UpdateBookingSupplierSelectionsAsync_RemovesOldVisits_WhenUpdatingExistingBooking() {
        var booking = TestHelper.CreateValidBooking();
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);

        var supplier1 = TestHelper.CreateSupplier(1);
        var supplier2 = TestHelper.CreateSupplier(2);
        var supplier3 = TestHelper.CreateSupplier(3);
        context.Suppliers.AddRange(supplier1, supplier2, supplier3);
        await context.SaveChangesAsync(CancellationToken);

        var oldVisit1 = new Visit { BookingId = booking.Id, SupplierId = supplier1.Id, IsClaimed = false };
        var oldVisit2 = new Visit { BookingId = booking.Id, SupplierId = supplier2.Id, IsClaimed = true };
        context.Visits.AddRange(oldVisit1, oldVisit2);
        await context.SaveChangesAsync(CancellationToken);

        var newSelectedSupplierIds = new List<int> { supplier3.Id };

        await sut.UpdateBookingSupplierSelectionsAsync(booking, newSelectedSupplierIds);

        var visits = await context.Visits
            .Where(v => v.BookingId == booking.Id)
            .ToListAsync(CancellationToken);

        visits.Count.ShouldBe(1);
        visits[0].SupplierId.ShouldBe(supplier3.Id);
        visits[0].IsClaimed.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateBookingSupplierSelectionsAsync_HandlesEmptySupplierList_WhenCalled() {
        var booking = TestHelper.CreateValidBooking();
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);

        var supplier1 = TestHelper.CreateSupplier(1);
        context.Suppliers.Add(supplier1);
        await context.SaveChangesAsync(CancellationToken);

        var oldBookingSupplier = new BookingSupplier { BookingId = booking.Id, SupplierId = supplier1.Id };
        context.BookingSuppliers.Add(oldBookingSupplier);
        await context.SaveChangesAsync(CancellationToken);

        var emptySupplierIds = new List<int>();

        await sut.UpdateBookingSupplierSelectionsAsync(booking, emptySupplierIds);

        var bookingSuppliers = await context.BookingSuppliers
            .Where(bs => bs.BookingId == booking.Id)
            .ToListAsync(CancellationToken);
        var visits = await context.Visits
            .Where(v => v.BookingId == booking.Id)
            .ToListAsync(CancellationToken);

        bookingSuppliers.Count.ShouldBe(0);
        visits.Count.ShouldBe(0);
        booking.SelectedSupplierIds.Count.ShouldBe(0);
    }
}
