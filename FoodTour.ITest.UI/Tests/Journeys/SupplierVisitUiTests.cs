namespace FoodTour.ITest.UI.Tests;

[Collection("FoodTour application")]
public class SupplierVisitUiTests(FoodTourApplicationFixture fixture) {
    private const string VISIT_CONFIRMATION_PATH = "/Home/VisitConfirmation";
    private const string VISIT_CONFIRMATION_URL_PATTERN = "**/Home/VisitConfirmation?*";

    [Fact]
    public async Task ConfirmVisit_NavigatesToVisitConfirmation_WhenSupplierCodeIsValid() {
        await using var browserContext = await fixture.CreateBrowserContextAsync();
        var page = await browserContext.NewPageAsync();
        var sut = new SupplierVisitPage(page);
        var booking = fixture.SeedData.SupplierVisitBooking;
        var firstVisit = fixture.SeedData.SupplierVisitBookingVisits.First();
        var supplier = fixture.SeedData.Suppliers.Single(seededSupplier => seededSupplier.Id == firstVisit.SupplierId);

        await sut.GotoAsync(booking.QrToken);
        await sut.ConfirmVisitAsync(supplier.Name, supplier.ConfirmationCode);

        await page.WaitForURLAsync(VISIT_CONFIRMATION_URL_PATTERN);
        page.Url.ShouldContain(VISIT_CONFIRMATION_PATH);
        var visitConfirmationPage = new VisitConfirmationPage(page);

        await Expect(visitConfirmationPage.SuccessHeading).ToBeVisibleAsync();
        await Expect(visitConfirmationPage.PreparationAlert).ToContainTextAsync(booking.TotalGuests.ToString());
    }

    [Fact]
    public async Task ConfirmVisit_ShowsAccessibleError_WhenSupplierCodeIsInvalid() {
        await using var browserContext = await fixture.CreateBrowserContextAsync();
        var page = await browserContext.NewPageAsync();
        var sut = new SupplierVisitPage(page);
        var booking = fixture.SeedData.SupplierVisitBooking;
        var firstVisit = fixture.SeedData.SupplierVisitBookingVisits.First();
        var supplier = fixture.SeedData.Suppliers.Single(seededSupplier => seededSupplier.Id == firstVisit.SupplierId);

        await sut.GotoAsync(booking.QrToken);
        await sut.ConfirmVisitWithInvalidCodeAsync(supplier.Name);

        await Expect(sut.SupplierErrorRegion).ToBeVisibleAsync();
        page.Url.ShouldContain("/Home/SupplierVisit");
    }
}
