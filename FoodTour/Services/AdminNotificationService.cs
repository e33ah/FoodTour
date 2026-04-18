namespace FoodTour.Services;

public class AdminNotificationService(IEmailSender emailSender, IOptions<AdminNotificationSettings> options, IRazorTemplateRenderer templateRenderer) {
    private const string TOUR_DATE_FORMAT = "dd/MM/yyyy";
    private const string CLAIMEDAT_DATE_FORMAT = "HH:mm dd/MM/yyyy";

    private readonly AdminNotificationSettings settings = options.Value;

    public async Task NotifyVisitRecordedAsync(Booking booking, Supplier supplier, Visit visit) {
        if (!settings.NotifyOnVisitRecorded) {
            Log.Debug("Admin email not sent as NotifyOnVisitRecorded is disabled.");
            return;
        }

        ValidateParameters(booking, supplier, visit);

        string claimedAt = visit.ClaimedAt!.Value.ToString(CLAIMEDAT_DATE_FORMAT);
        string subject = $"FoodTour: visit recorded for {WebUtility.HtmlEncode(supplier.Name)}";

        var model = new AdminVisitRecordedEmailModel {
            BookingRef = WebUtility.HtmlEncode(booking.PurchaseCode),
            CustomerEmail = WebUtility.HtmlEncode(booking.CustomerEmail),
            TourDate = booking.TourDate.ToString(TOUR_DATE_FORMAT),
            TotalGuests = WebUtility.HtmlEncode(booking.TotalGuests.ToString()),
            SupplierName = WebUtility.HtmlEncode(supplier.Name),
            ClaimedAt = claimedAt,
            Subject = subject
        };

        string body = await templateRenderer.RenderTemplateAsync("Emails/AdminVisitRecorded", model);

        await emailSender.SendEmailAsync(settings.AdminEmail!, subject, body);
    }

    private void ValidateParameters(Booking booking, Supplier supplier, Visit visit) {
        if (string.IsNullOrWhiteSpace(settings.AdminEmail)) {
            throw new InvalidOperationException("AdminNotificationSettings.AdminEmail is not configured.");
        }

        if (string.IsNullOrWhiteSpace(supplier.Name)) {
            throw new InvalidOperationException("Supplier.Name is required.");
        }

        if (string.IsNullOrWhiteSpace(booking.CustomerEmail)) {
            throw new InvalidOperationException("Booking.CustomerEmail is required.");
        }

        if (visit.ClaimedAt == null) {
            throw new InvalidOperationException("Visit.ClaimedAt is required.");
        }
    }
}
