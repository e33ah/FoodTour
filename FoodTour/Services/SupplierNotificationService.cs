namespace FoodTour.Services;

public class SupplierNotificationService(IEmailSender emailSender, IRazorTemplateRenderer templateRenderer) {
    private const string TOUR_DATE_FORMAT = "dd/MM/yyyy";
    private const string CLAIMEDAT_DATE_FORMAT = "HH:mm dd/MM/yyyy";

    public async Task NotifySupplierVisitRecordedAsync(Booking booking, Supplier supplier, Visit visit, string supportUrl) {
        if (!supplier.EmailNotificationsEnabled) {
            Log.Information("Visit ID {VisitId}: Supplier {SupplierName} opted out of notifications. Email not sent", visit.Id, supplier.Name);
            return;
        }

        ValidateParameters(supplier, visit, supportUrl);

        string[] recipientEmailAddresses = supplier.GetContactEmailAddresses();
        if (recipientEmailAddresses.Length == 0) {
            throw new InvalidOperationException("Supplier.ContactEmail is not configured.");
        }

        string claimedAt = visit.ClaimedAt!.Value.ToString(CLAIMEDAT_DATE_FORMAT);
        string subject = $"Food Tour: Visit recorded at {claimedAt}";

        var model = new SupplierVisitRecordedEmailModel {
            SupplierName = WebUtility.HtmlEncode(supplier.Name),
            BookingRef = WebUtility.HtmlEncode(booking.PurchaseCode),
            TourDate = booking.TourDate.ToString(TOUR_DATE_FORMAT),
            TotalGuests = WebUtility.HtmlEncode(booking.TotalGuests.ToString()),
            ClaimedAt = claimedAt,
            SupportUrl = supportUrl,
            Subject = subject
        };

        string body = await templateRenderer.RenderTemplateAsync("Emails/SupplierVisitRecorded", model);

        foreach (string recipientEmailAddress in recipientEmailAddresses) {
            await emailSender.SendEmailAsync(recipientEmailAddress, subject, body);
        }
    }

    private static void ValidateParameters(Supplier supplier, Visit visit, string supportUrl) {
        if (string.IsNullOrWhiteSpace(supplier.ContactEmail)) {
            throw new InvalidOperationException("Supplier.ContactEmail is not configured.");
        }

        if (string.IsNullOrWhiteSpace(supplier.Name)) {
            throw new InvalidOperationException("Supplier.Name is required.");
        }

        if (visit.ClaimedAt == null) {
            throw new InvalidOperationException("Visit.ClaimedAt is required.");
        }

        if (string.IsNullOrWhiteSpace(supportUrl)) {
            throw new InvalidOperationException("Support URL is required.");
        }
    }
}
