namespace FoodTour.Services;

public class AdminAccessService(IOptions<AdminSettings> options) : IAdminAccessService {
    private readonly AdminSettings settings = options.Value;

    public bool IsAllowed(string email) {
        if (string.IsNullOrWhiteSpace(email)) {
            return false;
        }

        return settings.AllowedEmails.Any(allowed =>
            string.Equals(allowed, email, StringComparison.OrdinalIgnoreCase));
    }
}
