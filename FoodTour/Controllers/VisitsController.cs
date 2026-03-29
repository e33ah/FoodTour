namespace FoodTour.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VisitsController(FoodTourContext context, AdminNotificationService adminNotificationService, SupplierNotificationService supplierNotificationService) : ControllerBase {

    [HttpPut("record")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordVisit([FromBody] RecordVisitDTO? payload) {
        var validationResult = await ValidateRecordVisitRequestAsync(payload);
        if (!validationResult.IsValid) {
            return BadRequest(new { error = validationResult.ErrorMessage });
        }

        var (booking, supplier, visit) = validationResult.Data!;

        try {
            int rows = await context.Visits
                .Where(v => v.Id == visit.Id && !v.IsClaimed)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(v => v.IsClaimed, true)
                    .SetProperty(v => v.ClaimedAt, v => DateTime.UtcNow));

            if (rows == 0) {
                Log.Warning("Booking {BookingId}, Supplier {SupplierId}: Visit already claimed.", booking.Id, payload!.SelectedSupplierId);
                return BadRequest(new { error = "This visit has already been confirmed." });
            }

            Log.Information("Booking {BookingId}, Supplier {SupplierId}: Visit recorded.", booking.Id, payload!.SelectedSupplierId);

            await context.Entry(visit).ReloadAsync();
        } catch (Exception ex) {
            Log.Error(ex, "Visit {VisitId}, Booking {BookingId}, Supplier {SupplierId}: Failed to save visit record.", visit.Id, booking.Id, payload!.SelectedSupplierId);
            return StatusCode(500, new { error = "Unable to record visit due to a system error." });
        }

        // Note: This method handles its own exceptions internally and logs any failures
        await TrySendVisitRecordedNotificationsAsync(booking, supplier, visit);

        return Ok(new { success = true });
    }

    public record RecordVisitDTO(Guid QrToken, int SelectedSupplierId, string SupplierCode);

    private record ValidationResult<T>(bool IsValid, string ErrorMessage, T? Data = default);

    private async Task<ValidationResult<(Booking booking, Supplier supplier, Visit visit)>> ValidateRecordVisitRequestAsync(RecordVisitDTO? payload) {
        if (!TryValidateRecordVisitPayload(payload, out string payloadError)) {
            Log.Warning("RecordVisit called with invalid parameters.");
            return new ValidationResult<(Booking, Supplier, Visit)>(false, payloadError);
        }

        var booking = await context.Bookings.SingleOrDefaultAsync(b => b.QrToken == payload!.QrToken && !b.IsCancelled);
        var supplier = await context.Suppliers.SingleOrDefaultAsync(s => s.Id == payload!.SelectedSupplierId && s.IsActive);

        if (booking == null) {
            Log.Warning("RecordVisit could not find active booking with QrToken: {QrToken}.", payload!.QrToken);
            return new ValidationResult<(Booking, Supplier, Visit)>(false, "Booking not found. Please contact Support.");
        }

        if (supplier == null) {
            Log.Warning("RecordVisit could not find active supplier with id {SupplierId}.", payload!.SelectedSupplierId);
            return new ValidationResult<(Booking, Supplier, Visit)>(false, "Business not found in Food Tour. Please contact Support.");
        }

        if (!string.Equals(supplier.ConfirmationCode, payload!.SupplierCode, StringComparison.OrdinalIgnoreCase)) {
            Log.Warning("RecordVisit supplier passcode mismatch for supplier id {SupplierId}.", payload.SelectedSupplierId);
            return new ValidationResult<(Booking, Supplier, Visit)>(false, "Invalid business code provided. Please try again or contact Support.");
        }

        var visit = await context.Visits
            .SingleOrDefaultAsync(v => v.BookingId == booking.Id && v.SupplierId == payload.SelectedSupplierId);

        if (visit == null) {
            Log.Warning("RecordVisit could not find visit for booking id {BookingId} and supplier id {SupplierId}.", booking.Id, payload.SelectedSupplierId);
            return new ValidationResult<(Booking, Supplier, Visit)>(false, "Visit not found for this booking and business. Please contact Support.");
        }

        return new ValidationResult<(Booking, Supplier, Visit)>(true, string.Empty, (booking, supplier, visit));
    }

    private static bool TryValidateRecordVisitPayload(RecordVisitDTO? payload, out string errorMessage) {
        if (payload == null || payload.SelectedSupplierId <= 0 || payload.QrToken == Guid.Empty || string.IsNullOrEmpty(payload.SupplierCode)) {
            errorMessage = "Invalid booking or business information.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
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
            string supportUrl = "/Home/UserSupport"; // Simplified for API controller
            await supplierNotificationService.NotifyVisitRecordedAsync(booking, supplier, visit, supportUrl);
            Log.Information("Visit ID {VisitId}: Supplier notification email sent to {SupplierEmail}", visit.Id, supplier.ContactEmail);
        } catch (Exception emailEx) {
            Log.Error(emailEx, "Visit ID {VisitId}: Visit recorded but failed to notify supplier. BookingId {BookingId}, SupplierId {SupplierId}.", visit.Id, booking.Id, supplier.Id);
        }
    }
}