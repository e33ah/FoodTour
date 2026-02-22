namespace FoodTour.Services;

public class AdminNotificationService(IEmailSender emailSender, IOptions<AdminNotificationSettings> options) {
    private readonly AdminNotificationSettings settings = options.Value;

    public async Task NotifyVisitRecordedAsync(Booking booking, Supplier supplier, Visit visit) {
        if (!settings.NotifyOnVisitRecorded) {
            Log.Debug("Admin email not sent as NotifyOnVisitRecorded is disabled.");
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.AdminEmail)) {
            throw new InvalidOperationException("AdminNotificationSettings.AdminEmail is not configured.");
        }
        if (AreParametersInvalid(booking, supplier, visit)) {
            throw new InvalidOperationException("Supplier name, customer email, or visit claim timestamp is missing or invalid");
        }

        string subject = $"FoodTour: visit recorded for {WebUtility.HtmlEncode(supplier.Name)}";
        string bookingId = WebUtility.HtmlEncode(booking.Id.ToString());
        string customerEmail = WebUtility.HtmlEncode(booking.CustomerEmail);
        string tourDate = booking.TourDate.ToString("dd/MM/yyyy");
        string supplierName = WebUtility.HtmlEncode(supplier.Name);
        string claimedAt = visit.ClaimedAt!.Value.ToString("HH:mm dd/MM/yyyy");

        string body = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset=""utf-8"" />
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
                <title>{subject}</title>
            </head>
            <body style=""font-family: Arial, sans-serif; font-size: 16px; line-height: 1.6;"">
                <p style=""margin: 0 0 12px 0;""><strong>Notification:</strong> A Food Tour visit has been recorded.</p>
                <p style=""margin: 0 0 8px 0;""><strong>Booking Id:</strong> {bookingId}</p>
                <p style=""margin: 0 0 8px 0;""><strong>Customer Email:</strong> {customerEmail}</p>
                <p style=""margin: 0 0 8px 0;""><strong>Tour Date:</strong> {tourDate}</p>
                <p style=""margin: 0 0 8px 0;""><strong>Supplier:</strong> {supplierName}</p>
                <p style=""margin: 0 0 8px 0;""><strong>Claimed At (UTC):</strong> {claimedAt}</p>

                <table style=""width: 100%; border-collapse: collapse; margin-top: 12px;"">
                    <thead>
                        <tr>
                            <th style=""border: 1px solid #e5e5e5; text-align: left; padding: 8px;""><strong>Booking Id</strong></th>
                            <th style=""border: 1px solid #e5e5e5; text-align: left; padding: 8px;""><strong>Customer Email</strong></th>
                            <th style=""border: 1px solid #e5e5e5; text-align: left; padding: 8px;""><strong>Tour Date</strong></th>
                            <th style=""border: 1px solid #e5e5e5; text-align: left; padding: 8px;""><strong>Supplier</strong></th>
                            <th style=""border: 1px solid #e5e5e5; text-align: left; padding: 8px;""><strong>Claimed At (UTC)</strong></th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr>
                            <td style=""border: 1px solid #e5e5e5; text-align: left; padding: 8px;"">{bookingId}</td>
                            <td style=""border: 1px solid #e5e5e5; text-align: left; padding: 8px;"">{customerEmail}</td>
                            <td style=""border: 1px solid #e5e5e5; text-align: left; padding: 8px;"">{tourDate}</td>
                            <td style=""border: 1px solid #e5e5e5; text-align: left; padding: 8px;"">{supplierName}</td>
                            <td style=""border: 1px solid #e5e5e5; text-align: left; padding: 8px;"">{claimedAt}</td>
                        </tr>
                    </tbody>
                </table>
            </body>
            </html>";

        await emailSender.SendEmailAsync(settings.AdminEmail, subject, body);
    }

    private static bool AreParametersInvalid(Booking booking, Supplier supplier, Visit visit) {
        return string.IsNullOrEmpty(supplier.Name)
            || string.IsNullOrEmpty(booking.CustomerEmail)
            || visit.ClaimedAt == null;
    }
}
