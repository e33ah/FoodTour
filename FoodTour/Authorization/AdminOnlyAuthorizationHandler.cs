namespace FoodTour.Authorization;

public class AdminOnlyAuthorizationHandler(IAdminAccessService adminAccessService)
    : AuthorizationHandler<AdminOnlyRequirement> {

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminOnlyRequirement requirement) {

        string? email = context.User.FindFirstValue(ClaimTypes.Email);

        if (email != null && adminAccessService.IsAllowed(email)) {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
