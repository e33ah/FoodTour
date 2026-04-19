namespace FoodTour.Services;

public class BookingEmailService(IRazorTemplateRenderer templateRenderer) {
    private const string TOUR_DATE_FORMAT = "dd/MM/yyyy";

    public async Task<(string Subject, string Body)> GenerateBookingConfirmationEmailAsync(Booking booking, IReadOnlyCollection<Supplier> suppliers, string foodTourCodeUrl, string userSupportUrl) {
        string subject = $"Weymouth Self-Guided Tasting Tour Booking Confirmation - {booking.TourDate.ToString(TOUR_DATE_FORMAT)}";

        var tastingExperiences = suppliers
            .Select(supplier => new TastingExperienceItem {
                Name = WebUtility.HtmlEncode(supplier.Name),
                Address = WebUtility.HtmlEncode(supplier.Address ?? string.Empty)
            })
            .ToList();

        var model = new BookingConfirmationEmailModel {
            CustomerName = string.IsNullOrWhiteSpace(booking.CustomerName)
                ? WebUtility.HtmlEncode(booking.CustomerEmail)
                : WebUtility.HtmlEncode(booking.CustomerName),
            TourDate = booking.TourDate.ToString(TOUR_DATE_FORMAT),
            TastingExperiences = tastingExperiences,
            FoodTourCodeUrl = foodTourCodeUrl,
            UserSupportUrl = userSupportUrl,
            Subject = subject
        };

        string body = await templateRenderer.RenderTemplateAsync("Emails/BookingConfirmation", model);

        return (subject, body);
    }

    public async Task<(string Subject, string Body)> GeneratePartnerBookingConfirmationEmailAsync(string customerEmail, string bookingReference, string validationUrl, string privacyPolicyUrl) {
        string subject = $"Confirm your Weymouth Self-Guided Tasting Tour booking - {bookingReference}";

        var model = new PartnerBookingConfirmationEmailModel {
            CustomerEmail = WebUtility.HtmlEncode(customerEmail),
            BookingReference = WebUtility.HtmlEncode(bookingReference),
            ValidationUrl = validationUrl,
            PrivacyPolicyUrl = privacyPolicyUrl,
            Subject = subject
        };

        string body = await templateRenderer.RenderTemplateAsync("Emails/PartnerBookingConfirmation", model);

        return (subject, body);
    }
}
