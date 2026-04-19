namespace FoodTour.Services;

public class PartnerBookingConfirmationService(FoodTourContext context, IDataProtectionProvider dataProtectionProvider) {
    private const int VALIDATION_TOKEN_LIFETIME_HOURS = 24;
    private const string PARTNER_BOOKING_CONFIRMATION_PURPOSE = "PartnerBookingConfirmation";
    private readonly IDataProtector partnerBookingConfirmationProtector = dataProtectionProvider.CreateProtector(PARTNER_BOOKING_CONFIRMATION_PURPOSE);

    public string CreateValidationToken(int bookingId, string customerEmail, string bookingReference, DateTime? expiresUtc = null) {
        var payload = new PartnerBookingValidationPayload {
            BookingId = bookingId,
            CustomerEmail = customerEmail,
            BookingReference = bookingReference,
            ExpiresUtc = expiresUtc ?? DateTime.UtcNow.AddHours(VALIDATION_TOKEN_LIFETIME_HOURS)
        };

        string serializedPayload = JsonSerializer.Serialize(payload);
        return partnerBookingConfirmationProtector.Protect(serializedPayload);
    }

    public async Task<PartnerBookingConfirmationResult> TryConfirmAsync(string validation) {
        if (string.IsNullOrWhiteSpace(validation)) {
            Log.Warning("CompletePartnerBookingConfirmation called without a validation token.");
            return PartnerBookingConfirmationResult.Invalid();
        }

        PartnerBookingValidationPayload? payload = null;
        try {
            string unprotectedPayload = partnerBookingConfirmationProtector.Unprotect(validation);
            payload = JsonSerializer.Deserialize<PartnerBookingValidationPayload>(unprotectedPayload);
        } catch (Exception ex) {
            Log.Warning(ex, "CompletePartnerBookingConfirmation received an invalid validation token.");
            return PartnerBookingConfirmationResult.Invalid();
        }

        if (payload == null || payload.ExpiresUtc < DateTime.UtcNow) {
            Log.Warning("CompletePartnerBookingConfirmation received an expired or empty validation token payload.");
            return PartnerBookingConfirmationResult.Expired(payload?.BookingReference, payload?.CustomerEmail);
        }

        var booking = await context.Bookings.FirstOrDefaultAsync(b =>
            b.Id == payload.BookingId &&
            b.VoucherNumber == payload.BookingReference &&
            !b.IsCancelled);

        if (booking == null) {
            Log.Warning("CompletePartnerBookingConfirmation could not find booking {BookingId}.", payload.BookingId);
            return PartnerBookingConfirmationResult.Invalid(payload.BookingReference, payload.CustomerEmail);
        }

        // This case occurs when a confirmation link is used after the booking's email has already been confirmed
        // (e.g., the user confirmed via another method or the link was used multiple times).
        if (!booking.RequiresEmailConfirmation) {
            return string.Equals(booking.CustomerEmail, payload.CustomerEmail, StringComparison.OrdinalIgnoreCase)
                ? PartnerBookingConfirmationResult.Success(booking)
                : PartnerBookingConfirmationResult.Invalid(payload.BookingReference, payload.CustomerEmail);
        }

        booking.CustomerEmail = payload.CustomerEmail;
        booking.RequiresEmailConfirmation = false;

        try {
            await context.SaveChangesAsync();
        } catch (Exception ex) {
            Log.Error(ex, "Booking {BookingId}: Failed to complete partner booking confirmation.", booking.Id);
            return PartnerBookingConfirmationResult.Invalid(payload.BookingReference, payload.CustomerEmail);
        }

        return PartnerBookingConfirmationResult.Success(booking);
    }

    public sealed class PartnerBookingConfirmationResult {
        public Booking? Booking { get; private init; }
        public string? BookingReference { get; private init; }
        public string? CustomerEmail { get; private init; }
        public PartnerBookingConfirmationStatus Status { get; private init; }

        public static PartnerBookingConfirmationResult Success(Booking booking) {
            return new PartnerBookingConfirmationResult {
                Booking = booking,
                BookingReference = booking.VoucherNumber ?? booking.PurchaseCode,
                CustomerEmail = booking.CustomerEmail,
                Status = PartnerBookingConfirmationStatus.Success
            };
        }

        public static PartnerBookingConfirmationResult Expired(string? bookingReference = null, string? customerEmail = null) {
            return new PartnerBookingConfirmationResult {
                BookingReference = bookingReference,
                CustomerEmail = customerEmail,
                Status = PartnerBookingConfirmationStatus.Expired
            };
        }

        public static PartnerBookingConfirmationResult Invalid(string? bookingReference = null, string? customerEmail = null) {
            return new PartnerBookingConfirmationResult {
                BookingReference = bookingReference,
                CustomerEmail = customerEmail,
                Status = PartnerBookingConfirmationStatus.Invalid
            };
        }
    }

    public enum PartnerBookingConfirmationStatus {
        Success,
        Expired,
        Invalid
    }

    private sealed class PartnerBookingValidationPayload {
        public int BookingId { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public string BookingReference { get; set; } = string.Empty;
        public DateTime ExpiresUtc { get; set; }
    }
}
