namespace FoodTour.Test.Services;

public class RazorTemplateRendererTests {
    private const string TEMPLATE_PATH = "Emails/BookingConfirmation";
    private readonly Mock<IRazorViewEngine> razorViewEngineMock = new(MockBehavior.Strict);
    private readonly Mock<ITempDataProvider> tempDataProviderMock = new(MockBehavior.Strict);
    private readonly Mock<IServiceProvider> serviceProviderMock = new(MockBehavior.Strict);
    private readonly Mock<IView> viewMock = new(MockBehavior.Strict);
    private readonly RazorTemplateRenderer sut;

    public RazorTemplateRendererTests() {
        sut = new RazorTemplateRenderer(razorViewEngineMock.Object, tempDataProviderMock.Object, serviceProviderMock.Object);
        razorViewEngineMock.Setup(engine => engine.FindView(It.IsAny<ActionContext>(), TEMPLATE_PATH, false))
            .Returns(ViewEngineResult.Found(TEMPLATE_PATH, viewMock.Object));
    }

    [Theory]
    [InlineData("<html><body>Booking confirmed</body></html>")]
    [InlineData("")]
    [InlineData("  Booking confirmed\r\n")]
    public async Task RenderTemplateAsync_ReturnsExactOutput_WhenViewRendersAsynchronously(string renderedOutput) {
        var model = TestHelper.CreateSupplier(1);
        var capturedContexts = new List<ViewContext>();
        viewMock.Setup(view => view.RenderAsync(It.IsAny<ViewContext>()))
            .Returns(async (ViewContext context) => {
                capturedContexts.Add(context);
                await Task.Yield();
                await context.Writer.WriteAsync(renderedOutput);
            });

        string result = await sut.RenderTemplateAsync(TEMPLATE_PATH, model);

        result.ShouldBe(renderedOutput);
        var viewContext = capturedContexts.ShouldHaveSingleItem();
        viewContext.View.ShouldBeSameAs(viewMock.Object);
        viewContext.ViewData.ShouldBeOfType<ViewDataDictionary<Supplier>>();
        viewContext.ViewData.Model.ShouldBeSameAs(model);
        viewContext.ModelState.ShouldBeEmpty();
        viewContext.HttpContext.ShouldBeOfType<DefaultHttpContext>();
        viewContext.HttpContext.RequestServices.ShouldBeSameAs(serviceProviderMock.Object);
        viewContext.RouteData.Values.ShouldBeEmpty();
        viewContext.ActionDescriptor.ShouldNotBeNull();
        razorViewEngineMock.Verify(engine => engine.FindView(
            It.Is<ActionContext>(context => context.HttpContext == viewContext.HttpContext),
            TEMPLATE_PATH, false), Times.Once);
        viewMock.Verify(view => view.RenderAsync(It.IsAny<ViewContext>()), Times.Once);
    }

    [Fact]
    public async Task RenderTemplateAsync_UsesTempDataProvider_WhenViewReadsTempData() {
        var tempDataValues = new Dictionary<string, object> { ["Message"] = "Booking confirmed" };
        tempDataProviderMock.Setup(provider => provider.LoadTempData(It.IsAny<HttpContext>()))
            .Returns(tempDataValues);
        viewMock.Setup(view => view.RenderAsync(It.IsAny<ViewContext>()))
            .Returns((ViewContext context) => context.Writer.WriteAsync((string?)context.TempData["Message"]));

        string result = await sut.RenderTemplateAsync(TEMPLATE_PATH, new object());

        result.ShouldBe("Booking confirmed");
        tempDataProviderMock.Verify(provider => provider.LoadTempData(
            It.Is<HttpContext>(context => context.RequestServices == serviceProviderMock.Object)), Times.Once);
    }

    [Theory]
    [InlineData("Jane Doe")]
    [InlineData(null)]
    public async Task RenderTemplateAsync_PassesTypedModel_WhenModelIsProvidedOrNull(string? model) {
        var capturedContexts = new List<ViewContext>();
        viewMock.Setup(view => view.RenderAsync(It.IsAny<ViewContext>()))
            .Callback<ViewContext>(capturedContexts.Add)
            .Returns(Task.CompletedTask);

        string result = await sut.RenderTemplateAsync(TEMPLATE_PATH, model);

        result.ShouldBeEmpty();
        var viewContext = capturedContexts.ShouldHaveSingleItem();
        var viewData = viewContext.ViewData.ShouldBeOfType<ViewDataDictionary<string>>();
        viewData.Model.ShouldBe(model);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RenderTemplateAsync_ThrowsWithSearchedLocations_WhenViewIsNotFound(bool hasSearchedLocations) {
        string[] searchedLocations = hasSearchedLocations
            ? ["/Views/Emails/BookingConfirmation.cshtml", "/Views/Shared/BookingConfirmation.cshtml"]
            : [];
        razorViewEngineMock.Setup(engine => engine.FindView(It.IsAny<ActionContext>(), TEMPLATE_PATH, false))
            .Returns(ViewEngineResult.NotFound(TEMPLATE_PATH, searchedLocations));
        string expectedLocations = hasSearchedLocations
            ? "/Views/Emails/BookingConfirmation.cshtml, /Views/Shared/BookingConfirmation.cshtml"
            : string.Empty;

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => sut.RenderTemplateAsync(TEMPLATE_PATH, new object()));

        exception.Message.ShouldBe($"Could not find view '{TEMPLATE_PATH}'. Searched locations: {expectedLocations}");
        razorViewEngineMock.Verify(engine => engine.FindView(It.IsAny<ActionContext>(), TEMPLATE_PATH, false), Times.Once);
        viewMock.Verify(view => view.RenderAsync(It.IsAny<ViewContext>()), Times.Never);
        tempDataProviderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RenderTemplateAsync_PropagatesException_WhenViewRenderingFails() {
        var expectedException = new InvalidOperationException("Template rendering failed.");
        viewMock.Setup(view => view.RenderAsync(It.IsAny<ViewContext>()))
            .ThrowsAsync(expectedException);

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => sut.RenderTemplateAsync(TEMPLATE_PATH, new object()));

        exception.ShouldBeSameAs(expectedException);
        viewMock.Verify(view => view.RenderAsync(It.IsAny<ViewContext>()), Times.Once);
    }
}
