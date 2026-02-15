namespace FoodTour.Services;

public class BookingEmailService {
    public (string Subject, string Body) GenerateBookingConfirmationEmail(Booking booking, IReadOnlyCollection<Supplier> suppliers, string foodTourCodeUrl) {

        string subject = $"Weymouth Food Tour Booking Confirmation - {booking.TourDate:dd/MM/yyyy}";

        string selectedTastingExperiencesHtml = suppliers.Count == 0
            ? "<li>No tasting experiences selected.</li>"
            : string.Join("", suppliers.Select(supplier => $"<li>{supplier.Name} - {supplier.Address}</li>"));

        string body = $@"<div style=""font-family: Arial, sans-serif; font-size: 12px; line-height: 1.4;"">
            <p>Dear {booking.CustomerEmail},</p>
            <p>Thank you for booking your Food Tasting Tour! Here are your booking details:</p>
            <ul>
                <li><strong>Tour Date:</strong> {booking.TourDate:dd/MM/yyyy}</li>
                <li><strong>Selected Tasting Experiences:</strong>
                    <ul>
                        {selectedTastingExperiencesHtml}
                    </ul>
                </li>
            </ul>
            <p>Please present your QR code on the day of your tour. You can access it using the link below:</p>
            <p>
                <a href=""{foodTourCodeUrl}"" style=""display: inline-block; padding: 10px 14px; background-color: #0d6efd; color: #ffffff; text-decoration: none; border-radius: 6px;"">Open your Food Tour Code Details</a>
            </p>
            <p>If the button does not work, copy and paste this link into your browser:</p>
            <p><a href=""{foodTourCodeUrl}"">{foodTourCodeUrl}</a></p>
            <p>We look forward to seeing you!</p>
            <p>Best regards,<br>Kabincraft Adventures</p>
        </div>";

        return (subject, body);
    }
}