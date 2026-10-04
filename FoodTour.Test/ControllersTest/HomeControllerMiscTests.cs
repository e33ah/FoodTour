namespace FoodTour.Test.Controllers;

public class HomeControllerMiscTests : HomeControllerTestBase {
    [Fact]
    public void Privacy_ReturnsViewResult() {
        var sut = CreateController();

        var result = sut.Privacy();

        result.ShouldBeOfType<ViewResult>();
    }

    [Theory]
    [InlineData("TestTraceId", true)]
    [InlineData("", false)]
    public void Error_ReturnsViewResult_WithErrorViewModel(string traceId, bool showRequestId) {
        var sut = CreateController();
        var httpContext = new DefaultHttpContext { TraceIdentifier = traceId };
        sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = sut.Error();

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<ErrorViewModel>();
        model.RequestId.ShouldBe(traceId);
        model.ShowRequestId.ShouldBe(showRequestId);
    }

    [Fact]
    public void VisitConfirmation_ReturnsViewResult() {
        var sut = CreateController();

        var result = sut.VisitConfirmation(2, 3);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<VisitConfirmationVM>();
        model.TastingExperiencesClaimed.ShouldBe(2);
        model.TotalGuests.ShouldBe(3);
    }

    [Fact]
    public void TourCompleted_ReturnsViewResult() {
        var sut = CreateController();

        var result = sut.TourCompleted();

        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public void UserSupport_ReturnsViewResult() {
        var sut = CreateController();

        var result = sut.UserSupport();

        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeOfType<UserSupportVM>();
    }
}