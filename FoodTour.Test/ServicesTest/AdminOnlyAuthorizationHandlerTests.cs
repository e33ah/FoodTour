namespace FoodTour.Test.Services;

public class AdminOnlyAuthorizationHandlerTests {
    [Fact]
    public async Task HandleAsync_Succeeds_WhenUserEmailIsAllowed() {
        var adminAccessServiceMock = new Mock<IAdminAccessService>();
        adminAccessServiceMock.Setup(x => x.IsAllowed(TestHelper.ADMIN_EMAIL)).Returns(true);
        var sut = new AdminOnlyAuthorizationHandler(adminAccessServiceMock.Object);
        var requirement = new AdminOnlyRequirement();
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Email, TestHelper.ADMIN_EMAIL)
        ], "TestAuth"));
        var context = new AuthorizationHandlerContext([requirement], user, null);

        await sut.HandleAsync(context);

        context.HasSucceeded.ShouldBeTrue();
        adminAccessServiceMock.Verify(x => x.IsAllowed(TestHelper.ADMIN_EMAIL), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DoesNotSucceed_WhenUserEmailIsNotAllowed() {
        var adminAccessServiceMock = new Mock<IAdminAccessService>();
        adminAccessServiceMock.Setup(x => x.IsAllowed(TestHelper.ADMIN_EMAIL)).Returns(false);
        var sut = new AdminOnlyAuthorizationHandler(adminAccessServiceMock.Object);
        var requirement = new AdminOnlyRequirement();
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Email, TestHelper.ADMIN_EMAIL)
        ], "TestAuth"));
        var context = new AuthorizationHandlerContext([requirement], user, null);

        await sut.HandleAsync(context);

        context.HasSucceeded.ShouldBeFalse();
        adminAccessServiceMock.Verify(x => x.IsAllowed(TestHelper.ADMIN_EMAIL), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DoesNotSucceed_WhenUserEmailClaimIsMissing() {
        var adminAccessServiceMock = new Mock<IAdminAccessService>();
        var sut = new AdminOnlyAuthorizationHandler(adminAccessServiceMock.Object);
        var requirement = new AdminOnlyRequirement();
        var user = new ClaimsPrincipal(new ClaimsIdentity([], "TestAuth"));
        var context = new AuthorizationHandlerContext([requirement], user, null);

        await sut.HandleAsync(context);

        context.HasSucceeded.ShouldBeFalse();
        adminAccessServiceMock.Verify(x => x.IsAllowed(It.IsAny<string>()), Times.Never);
    }
}
