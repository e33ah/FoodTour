namespace FoodTour.ITest.API.Tests;

[Collection("FoodTour API application")]
public class AntiForgeryHelperApiTests(ApiApplicationFixture fixture) : ApiTestBase(fixture) {
    [Fact]
    public async Task GetRequestDataAsync_ReturnsTokenAndCookies_WhenSupplierVisitPageLoads() {
        using var sut = CreateApiClient();
        var booking = Fixture.SeedData.SupplierVisitBooking;

        var result = await AntiForgeryTokenHelper.GetRequestDataAsync(
            sut,
            CreateSupplierVisitPath(booking.QrToken),
            TestContext.Current.CancellationToken);

        result.RequestToken.ShouldNotBeNullOrWhiteSpace();
        result.CookieHeader.ShouldContain("=");
    }

    [Fact]
    public async Task PostSupplierVisit_ReturnsRedirect_WhenAntiForgeryDataIsIncluded() {
        using var sut = CreateApiClient();
        var booking = Fixture.SeedData.SupplierVisitBooking;
        var visit = Fixture.SeedData.SupplierVisitBookingVisits[0];
        var supplier = Fixture.SeedData.Suppliers.Single(seededSupplier => seededSupplier.Id == visit.SupplierId);
        var antiForgeryData = await AntiForgeryTokenHelper.GetRequestDataAsync(
            sut,
            CreateSupplierVisitPath(booking.QrToken),
            TestContext.Current.CancellationToken);
        var formData = new Dictionary<string, string> {
            ["qrToken"] = booking.QrToken.ToString(),
            ["selectedSupplierId"] = supplier.Id.ToString(),
            ["supplierCode"] = supplier.ConfirmationCode,
            ["tastingExperiencesClaimed"] = booking.TotalGuests.ToString(),
            ["__RequestVerificationToken"] = antiForgeryData.RequestToken
        };
        HttpRequestMessage request = new(HttpMethod.Post, "/Home/SupplierVisit") {
            Content = new FormUrlEncodedContent(formData)
        };
        request.Headers.Add("Cookie", antiForgeryData.CookieHeader);

        var response = await sut.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        var result = await GetVisitAsync(booking.Id, supplier.Id);
        result.IsClaimed.ShouldBeTrue();
    }
}
