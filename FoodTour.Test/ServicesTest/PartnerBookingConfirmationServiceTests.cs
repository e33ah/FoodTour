namespace FoodTour.Test.Services;

public class PartnerBookingConfirmationServiceTests : TestBase {
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TryConfirmAsync_ReturnsInvalid_WhenTokenIsBlank(string validation) {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = TestHelper.CreatePartnerBookingConfirmationService(context);

        var result = await sut.TryConfirmAsync(validation);

        result.Status.ShouldBe(PartnerBookingConfirmationService.PartnerBookingConfirmationStatus.Invalid);
        result.Booking.ShouldBeNull();
    }

    [Fact]
    public async Task TryConfirmAsync_ReturnsInvalid_WhenTokenCannotBeUnprotected() {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = TestHelper.CreatePartnerBookingConfirmationService(context);

        var result = await sut.TryConfirmAsync("invalid-token");

        result.Status.ShouldBe(PartnerBookingConfirmationService.PartnerBookingConfirmationStatus.Invalid);
        result.Booking.ShouldBeNull();
    }

    [Fact]
    public async Task TryConfirmAsync_ReturnsExpired_WhenTokenHasExpired() {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = TestHelper.CreatePartnerBookingConfirmationService(context);
        string validation = sut.CreateValidationToken(
            123,
            TestHelper.UPDATED_EMAIL,
            TestHelper.VALID_VOUCHER,
            DateTime.UtcNow.AddMinutes(-1));

        var result = await sut.TryConfirmAsync(validation);

        result.Status.ShouldBe(PartnerBookingConfirmationService.PartnerBookingConfirmationStatus.Expired);
        result.BookingReference.ShouldBe(TestHelper.VALID_VOUCHER);
        result.CustomerEmail.ShouldBe(TestHelper.UPDATED_EMAIL);
    }

    [Fact]
    public async Task TryConfirmAsync_UpdatesEmailAndConfirmsBooking_WhenTokenMatchesPendingBooking() {
        using var context = TestHelper.CreateInMemoryContext();
        var booking = TestHelper.CreateValidBooking([]);
        booking.VoucherNumber = TestHelper.VALID_VOUCHER;
        booking.RequiresEmailConfirmation = true;
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var sut = TestHelper.CreatePartnerBookingConfirmationService(context);
        string validation = sut.CreateValidationToken(booking.Id, TestHelper.UPDATED_EMAIL, TestHelper.VALID_VOUCHER);

        var result = await sut.TryConfirmAsync(validation);

        result.Status.ShouldBe(PartnerBookingConfirmationService.PartnerBookingConfirmationStatus.Success);
        var confirmedBooking = result.Booking ?? throw new InvalidOperationException("Expected a confirmed booking.");
        confirmedBooking.CustomerEmail.ShouldBe(TestHelper.UPDATED_EMAIL);
        confirmedBooking.RequiresEmailConfirmation.ShouldBeFalse();
        result.BookingReference.ShouldBe(TestHelper.VALID_VOUCHER);
        context.ChangeTracker.Clear();
        var savedBooking = await context.Bookings.SingleAsync(item => item.Id == booking.Id, CancellationToken);
        savedBooking.CustomerEmail.ShouldBe(TestHelper.UPDATED_EMAIL);
        savedBooking.RequiresEmailConfirmation.ShouldBeFalse();
    }

    [Fact]
    public async Task TryConfirmAsync_ReturnsInvalid_WhenBookingDoesNotMatchToken() {
        using var context = TestHelper.CreateInMemoryContext();
        var sut = TestHelper.CreatePartnerBookingConfirmationService(context);
        string validation = sut.CreateValidationToken(123, TestHelper.UPDATED_EMAIL, TestHelper.VALID_VOUCHER);

        var result = await sut.TryConfirmAsync(validation);

        result.Status.ShouldBe(PartnerBookingConfirmationService.PartnerBookingConfirmationStatus.Invalid);
        result.Booking.ShouldBeNull();
        result.BookingReference.ShouldBe(TestHelper.VALID_VOUCHER);
        result.CustomerEmail.ShouldBe(TestHelper.UPDATED_EMAIL);
    }

    [Fact]
    public async Task TryConfirmAsync_ReturnsInvalid_WhenBookingIdMatchesButVoucherDoesNot() {
        using var context = TestHelper.CreateInMemoryContext();
        var booking = TestHelper.CreateValidBooking([]);
        booking.VoucherNumber = "OTHER-VOUCHER";
        booking.CustomerEmail = TestHelper.UPDATED_EMAIL;
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var sut = TestHelper.CreatePartnerBookingConfirmationService(context);
        string validation = sut.CreateValidationToken(booking.Id, TestHelper.UPDATED_EMAIL, TestHelper.VALID_VOUCHER);

        var result = await sut.TryConfirmAsync(validation);

        result.Status.ShouldBe(PartnerBookingConfirmationService.PartnerBookingConfirmationStatus.Invalid);
        result.Booking.ShouldBeNull();
    }

    [Fact]
    public async Task TryConfirmAsync_ReturnsInvalid_WhenVoucherMatchesButBookingIdDoesNot() {
        using var context = TestHelper.CreateInMemoryContext();
        var booking = TestHelper.CreateValidBooking([]);
        booking.VoucherNumber = TestHelper.VALID_VOUCHER;
        booking.CustomerEmail = TestHelper.UPDATED_EMAIL;
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var sut = TestHelper.CreatePartnerBookingConfirmationService(context);
        string validation = sut.CreateValidationToken(booking.Id + 1, TestHelper.UPDATED_EMAIL, TestHelper.VALID_VOUCHER);

        var result = await sut.TryConfirmAsync(validation);

        result.Status.ShouldBe(PartnerBookingConfirmationService.PartnerBookingConfirmationStatus.Invalid);
        result.Booking.ShouldBeNull();
    }

    [Fact]
    public async Task TryConfirmAsync_ReturnsInvalid_WhenMatchingBookingIsCancelled() {
        using var context = TestHelper.CreateInMemoryContext();
        var booking = TestHelper.CreateValidBooking([]);
        booking.VoucherNumber = TestHelper.VALID_VOUCHER;
        booking.IsCancelled = true;
        booking.CustomerEmail = TestHelper.UPDATED_EMAIL;
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var sut = TestHelper.CreatePartnerBookingConfirmationService(context);
        string validation = sut.CreateValidationToken(booking.Id, TestHelper.UPDATED_EMAIL, TestHelper.VALID_VOUCHER);

        var result = await sut.TryConfirmAsync(validation);

        result.Status.ShouldBe(PartnerBookingConfirmationService.PartnerBookingConfirmationStatus.Invalid);
        result.Booking.ShouldBeNull();
    }

    [Fact]
    public async Task TryConfirmAsync_ReturnsSuccess_WhenEmailWasAlreadyConfirmedIgnoringCase() {
        using var context = TestHelper.CreateInMemoryContext();
        var booking = TestHelper.CreateValidBooking([]);
        booking.VoucherNumber = TestHelper.VALID_VOUCHER;
        booking.RequiresEmailConfirmation = false;
        booking.CustomerEmail = TestHelper.UPDATED_EMAIL;
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var sut = TestHelper.CreatePartnerBookingConfirmationService(context);
        string validation = sut.CreateValidationToken(booking.Id, TestHelper.UPDATED_EMAIL.ToUpperInvariant(), TestHelper.VALID_VOUCHER);

        var result = await sut.TryConfirmAsync(validation);

        result.Status.ShouldBe(PartnerBookingConfirmationService.PartnerBookingConfirmationStatus.Success);
        var confirmedBooking = result.Booking ?? throw new InvalidOperationException("Expected a confirmed booking.");
        confirmedBooking.ShouldBe(booking);
        confirmedBooking.CustomerEmail.ShouldBe(TestHelper.UPDATED_EMAIL);
    }

    [Fact]
    public async Task TryConfirmAsync_ReturnsInvalid_WhenAlreadyConfirmedEmailDoesNotMatchToken() {
        using var context = TestHelper.CreateInMemoryContext();
        var booking = TestHelper.CreateValidBooking([]);
        booking.VoucherNumber = TestHelper.VALID_VOUCHER;
        booking.RequiresEmailConfirmation = false;
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var sut = TestHelper.CreatePartnerBookingConfirmationService(context);
        string validation = sut.CreateValidationToken(booking.Id, TestHelper.UPDATED_EMAIL, TestHelper.VALID_VOUCHER);

        var result = await sut.TryConfirmAsync(validation);

        result.Status.ShouldBe(PartnerBookingConfirmationService.PartnerBookingConfirmationStatus.Invalid);
        result.Booking.ShouldBeNull();
    }
}
