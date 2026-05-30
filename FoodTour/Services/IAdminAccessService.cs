namespace FoodTour.Services;

public interface IAdminAccessService {
    bool IsAllowed(string email);
}
