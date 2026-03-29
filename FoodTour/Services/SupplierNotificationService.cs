namespace FoodTour.Services;

public class SupplierNotificationService(IEmailSender emailSender) {
    private const string TOUR_DATE_FORMAT = "dd/MM/yyyy";
    private const string CLAIMEDAT_DATE_FORMAT = "HH:mm dd/MM/yyyy";

    public async Task NotifyVisitRecordedAsync(Booking booking, Supplier supplier, Visit visit, string supportUrl) {
        if (!supplier.EmailNotificationsEnabled) {
            Log.Information("Visit ID {VisitId}: Supplier {SupplierName} opted out of notifications. Email not sent", visit.Id, supplier.Name);
            return;
        }

        if (string.IsNullOrWhiteSpace(supplier.ContactEmail)) {
            throw new InvalidOperationException("Supplier.ContactEmail is not configured.");
        }

        if (string.IsNullOrWhiteSpace(supplier.Name) || visit.ClaimedAt == null) {
            throw new InvalidOperationException("Parameter details not valid");
        }


        string bookingId = WebUtility.HtmlEncode(booking.Id.ToString());
        string tourDate = booking.TourDate.ToString(TOUR_DATE_FORMAT);
        string supplierName = WebUtility.HtmlEncode(supplier.Name);
        string claimedAt = visit.ClaimedAt.Value.ToString(CLAIMEDAT_DATE_FORMAT);
        if (string.IsNullOrWhiteSpace(supportUrl)) {
            throw new InvalidOperationException("Support URL is required.");
        }
        string subject = $"Food Tour: Visit recorded at {claimedAt}";

        string body = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset=""utf-8"" />
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
                <title>{subject}</title>
            </head>
            <body style=""font-family: Arial, sans-serif; font-size: 16px; line-height: 1.6;"">
                <p style=""margin: 0 0 12px 0;"">Dear {supplierName},</p>
                <p style=""margin: 0 0 12px 0;"">A Tasting Tour visit has been recorded at your venue.</p>
                <p style=""margin: 0 0 8px 0;""><strong>Booking Id:</strong> {bookingId}</p>
                <p style=""margin: 0 0 8px 0;""><strong>Tour Date:</strong> {tourDate}</p>
                <p style=""margin: 0 0 8px 0;""><strong>Claimed At (UTC):</strong> {claimedAt}</p>
                <p style=""margin: 12px 0 0 0;"">If you require any assistance, please visit our <a href=""{supportUrl}"" style=""color: #0d6efd;"">Support</a> page.</p>
                <p style=""margin: 12px 0 0 0;"">Kabincraft Adventures will be in contact shortly to reimburse funds.</p>
                <p style=""margin: 16px 0 0 0;"">Kind regards,<br /><strong>Kabincraft Adventures</strong></p>
            </body>
            </html>";

        await emailSender.SendEmailAsync(supplier.ContactEmail, subject, body);
    }
}
