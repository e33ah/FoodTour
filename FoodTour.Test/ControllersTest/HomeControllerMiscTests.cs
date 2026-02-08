namespace FoodTour.Test.Controllers;

public class HomeControllerMiscTests : HomeControllerTestBase {
    [Fact]
    public void Privacy_ReturnsViewResult() {
        var sut = CreateController();

        var result = sut.Privacy();

        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public void Error_ReturnsViewResult_WithErrorViewModel() {
        var sut = CreateController();
        var httpContext = new DefaultHttpContext { TraceIdentifier = "TestTraceId" };
        sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = sut.Error();

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<ErrorViewModel>();
        model.RequestId.ShouldBe("TestTraceId");
    }

    [Fact]
    public void VisitConfirmation_ReturnsViewResult() {
        var sut = CreateController();

        var result = sut.VisitConfirmation();

        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public void TourCompleted_ReturnsViewResult() {
        var sut = CreateController();

        var result = sut.TourCompleted();

        result.ShouldBeOfType<ViewResult>();
    }
}