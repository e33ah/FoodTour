namespace FoodTour.ITest.API.Tests;

[Collection("FoodTour API application")]
public class WebhookAuthenticationApiTests(ApiApplicationFixture fixture) : ApiTestBase(fixture) {
    [Theory]
    [InlineData("newbooking")]
    [InlineData("updatebooking")]
    [InlineData("cancelbooking")]
    public async Task Webhook_ReturnsUnauthorised_WhenKeyIsMissingOrInvalid(string action) {
        using var sut = CreateApiClient();
        string?[] invalidKeys = [null, string.Empty, " \t ", "invalid", FoodTourApiRequestFactory.ValidKey.ToUpperInvariant()];

        foreach (string? key in invalidKeys) {
            string query = key is null ? string.Empty : $"?key={Uri.EscapeDataString(key)}";
            using var content = new StringContent("{}", Encoding.UTF8, "application/json");

            using var result = await sut.PostAsync($"/api/foodtour/fhwebhook/{action}{query}", content, TestContext.Current.CancellationToken);

            result.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }
}
