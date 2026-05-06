namespace FoodTour.ITest.API.Tests;

[Collection("FoodTour API application")]
public class AntiForgeryHelperApiTests(ApiApplicationFixture fixture) : ApiTestBase(fixture) {
    [Fact]
    public async Task GetRequestDataAsync_ReturnsTokenAndCookies_WhenSupplierVisitPageLoads() {
        using var sut = CreateApiClient();
        var booking = Fixture.SeedData.SupplierVisitBooking;

        var result = await AntiForgeryTokenHelper.GetRequestDataAsync(
            sut,
            VisitsApiRequestFactory.CreateSupplierVisitPath(booking.QrToken),
            TestContext.Current.CancellationToken);

        result.RequestToken.ShouldNotBeNullOrWhiteSpace();
        result.CookieHeader.ShouldContain("=");
    }

    [Fact]
    public async Task PutRecordVisitAsync_ReturnsOk_WhenAntiForgeryDataIsIncluded() {
        using var sut = CreateApiClient();
        var booking = Fixture.SeedData.SupplierVisitBooking;
        var visit = Fixture.SeedData.SupplierVisitBookingVisits[0];
        var supplier = Fixture.SeedData.Suppliers.Single(seededSupplier => seededSupplier.Id == visit.SupplierId);
        RecordVisitRequest request = new() {
            QrToken = booking.QrToken,
            SelectedSupplierId = supplier.Id,
            SupplierCode = supplier.ConfirmationCode,
            TastingExperiencesClaimed = booking.TotalGuests
        };

        var response = await PutRecordVisitAsync(sut, request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await GetVisitAsync(booking.Id, supplier.Id);
        result.IsClaimed.ShouldBeTrue();
    }
}
