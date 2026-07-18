namespace FoodTour.Test.Controllers;

public class HomeControllerIndexTests : HomeControllerTestBase {
    [Fact]
    public async Task Index_ReturnsViewResult_WithActiveSuppliers() {
        string name1 = "The Enchanted Spoon";
        string name2 = "Whisker's Tea Rooms";
        var suppliers = new[] {
            TestHelper.CreateSupplier(1, name1, address: "123 Test St"),
            TestHelper.CreateSupplier(2, name2, address: "456 Test Ave")
        };

        var (_, model) = await GetIndexViewModel(suppliers);

        model.Suppliers.Count.ShouldBe(2);
        model.Suppliers[0].Name.ShouldBe(name1);
        model.Suppliers[1].Name.ShouldBe(name2);
    }

    [Fact]
    public async Task Index_ReturnsViewResult_WithOnlyActiveSuppliers() {
        var suppliers = new[] {
            TestHelper.CreateSupplier(1, "Active Supplier", isActive: true),
            TestHelper.CreateSupplier(2, "Inactive Supplier", isActive: false)
        };

        var (_, model) = await GetIndexViewModel(suppliers);

        model.Suppliers.Count.ShouldBe(1);
        model.Suppliers[0].Name.ShouldBe("Active Supplier");
        model.Suppliers[0].IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Index_SetsMaxSelections_InViewModel() {
        var (_, model) = await GetIndexViewModel();

        model.MaxSelections.ShouldBe(0);
    }

    [Fact]
    public async Task Index_SetsNoSuppliersMessage_WhenNoActiveSuppliers() {
        var (_, model) = await GetIndexViewModel();

        model.Suppliers.Count.ShouldBe(0);
        model.NoSuppliersMessage.ShouldBe(TestHelper.EXPECTED_NO_SUPPLIERS_MESSAGE);
    }

    [Fact]
    public async Task Index_DoesNotSetMessages_WhenAtLeastMaxActiveSuppliersExist() {
        var (_, model) = await GetIndexViewModel(supplierCount: TestHelper.EXPECTED_MAX_SELECTIONS);
        model.InsufficientSuppliersWarning.ShouldBeNull();
        model.NoSuppliersMessage.ShouldBeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task Index_SetsInsufficientSuppliersWarning_WhenFewerThanMaxActiveSuppliers(int supplierCount) {
        var (_, model) = await GetIndexViewModel(supplierCount: supplierCount);

        model.Suppliers.Count.ShouldBe(supplierCount);

        string experienceText = supplierCount == 1 ? "tasting experience is" : "tasting experiences are";
        string expectedMessage = $"Only {supplierCount} {experienceText} currently available. You need at least {TestHelper.EXPECTED_MAX_SELECTIONS} to create a tour. Please check back later.";

        model.InsufficientSuppliersWarning.ShouldBe(expectedMessage);
        model.NoSuppliersMessage.ShouldBeNull();
    }

    [Fact]
    public async Task Index_DoesNotSetInsufficientSuppliersWarning_WhenExactlyMaxActiveSuppliers() {
        var (_, model) = await GetIndexViewModel(supplierCount: TestHelper.EXPECTED_MAX_SELECTIONS);

        model.InsufficientSuppliersWarning.ShouldBeNull();
    }

    [Fact]
    public async Task Index_PopulatesSelectedSuppliers_WhenQrTokenProvided() {
        var selectedSupplierIds = new List<int> { 1, 2, 3 };
        var qrToken = Guid.NewGuid();

        var context = TestHelper.CreateContextWithActiveSuppliersCount(3);
        var booking = await TestHelper.CreateBookingInDatabase(context, selectedSupplierIds: selectedSupplierIds);
        booking.QrToken = qrToken;
        await context.SaveChangesAsync(CancellationToken);

        var sut = TestHelper.CreateHomeController(context);

        var result = await sut.Index(qrToken);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<FoodTourVM>();
        model.SelectedSuppliers.ShouldBe(selectedSupplierIds);
    }

    [Fact]
    public async Task Index_HandlesEmptyQrToken_WithoutErrors() {
        var context = TestHelper.CreateInMemoryContext();
        var sut = TestHelper.CreateHomeController(context);

        var result = await sut.Index(qrToken: null);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<FoodTourVM>();

        model.ShouldNotBeNull();
        model.Suppliers.ShouldNotBeNull();
    }

    [Fact]
    public async Task Index_HandlesInvalidQrToken_WithoutErrors() {
        var context = TestHelper.CreateInMemoryContext();
        var sut = TestHelper.CreateHomeController(context);
        var invalidToken = Guid.NewGuid();

        var result = await sut.Index(qrToken: invalidToken);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<FoodTourVM>();

        model.ShouldNotBeNull();
        model.Suppliers.ShouldNotBeNull();
    }
    private static async Task<(ViewResult viewResult, FoodTourVM model)> GetIndexViewModel(params Supplier[] suppliers) {
        var context = suppliers.Length > 0
            ? TestHelper.CreateContextWithSuppliers(suppliers)
            : TestHelper.CreateInMemoryContext();
        var sut = TestHelper.CreateHomeController(context);


        var result = await sut.Index(qrToken: null);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<FoodTourVM>();
        return (viewResult, model);
    }

    private static async Task<(ViewResult viewResult, FoodTourVM model)> GetIndexViewModel(int supplierCount) {
        var suppliers = Enumerable.Range(1, supplierCount)
            .Select(i => TestHelper.CreateSupplier(i, $"Test Supplier {i}"))
            .ToArray();
        return await GetIndexViewModel(suppliers);
    }

    [Fact]
    public async Task Index_Post_ReturnsViewWithErrorAndPreservesEnteredDetails_WhenBookingIdIsInvalid() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        var sut = CreateConfiguredController(context);
        sut.ControllerContext.HttpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues> {
            ["customerEmail"] = TestHelper.VALID_EMAIL,
            ["purchaseCode"] = TestHelper.VALID_CODE
        });
        var selectedSuppliers = new List<int> { 1, 2, 3, 4, 5 };

        var result = await sut.Index(0, selectedSuppliers);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<FoodTourVM>();
        model.CustomerEmail.ShouldBe(TestHelper.VALID_EMAIL);
        model.PurchaseCode.ShouldBe(TestHelper.VALID_CODE);
        sut.ModelState.ContainsKey(string.Empty).ShouldBeTrue();
        sut.ModelState[string.Empty]!.Errors[0].ErrorMessage.ShouldContain("could not be validated");
    }

    [Fact]
    public async Task Index_Post_RedirectsToFoodTourCode_WhenBookingIsAlreadyConfirmed() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        var booking = TestHelper.CreateValidBooking([1, 2, 3, 4, 5]);
        booking.IsConfirmed = true;
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = new List<int> { 1, 2, 3, 4, 5 };

        var result = await sut.Index(booking.Id, selectedSuppliers);

        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(HomeController.FoodTourCode));
        redirectResult.RouteValues.ShouldNotBeNull();
        redirectResult.RouteValues["qrToken"].ShouldBe(booking.QrToken);
    }

    [Fact]
    public async Task Index_Post_RedirectsToTourCompleted_WhenBookingIsAlreadyConfirmedAndAllVisitsAreClaimed() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        var booking = TestHelper.CreateValidBooking([1, 2, 3, 4, 5]);
        booking.IsConfirmed = true;
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        context.Visits.AddRange(booking.SelectedSupplierIds.Select(supplierId => new Visit {
            BookingId = booking.Id,
            SupplierId = supplierId,
            IsClaimed = true
        }));
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = new List<int> { 1, 2, 3, 4, 5 };

        var result = await sut.Index(booking.Id, selectedSuppliers);

        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(HomeController.TourCompleted));
    }

    [Fact]
    public async Task Index_Post_RedirectsToTourCancelled_WhenBookingIsCancelled() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        var booking = TestHelper.CreateValidBooking([1, 2, 3, 4, 5]);
        booking.IsCancelled = true;
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = new List<int> { 1, 2, 3, 4, 5 };

        var result = await sut.Index(booking.Id, selectedSuppliers);

        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(HomeController.TourCancelled));
        redirectResult.RouteValues.ShouldNotBeNull();
        redirectResult.RouteValues["customerEmail"].ShouldBe(booking.CustomerEmail);
        redirectResult.RouteValues["tourDate"].ShouldBe(booking.TourDate);
        redirectResult.RouteValues["purchaseCode"].ShouldBe(booking.PurchaseCode);
    }
}
