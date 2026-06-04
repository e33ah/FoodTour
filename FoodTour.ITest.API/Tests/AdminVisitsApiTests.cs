namespace FoodTour.ITest.API.Tests;

[Collection("FoodTour API application")]
public sealed class AdminVisitsApiTests(ApiApplicationFixture fixture) : ApiTestBase(fixture) {
    [Fact]
    public async Task Visits_ReturnsOk_WhenAuthenticated() {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/Admin/Visits", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Visits_ReturnsVisitsList_WithCorrectData() {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/Admin/Visits", TestContext.Current.CancellationToken);
        string html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain("Visits");
        Fixture.SeedData.SupplierVisitBookingVisits.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Visits_RedirectsToLogin_WhenNotAuthenticated() {
        using var client = CreateApiClient();

        var response = await client.GetAsync("/Admin/Visits", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().ShouldContain("/Admin/Login");
    }

    [Fact]
    public async Task MarkReimbursed_UpdatesDatabase_WhenVisitExists() {
        using var client = CreateAuthenticatedClient();
        int visitId = Fixture.SeedData.SupplierVisitBookingVisits[3].Id;

        var antiForgeryData = await AntiForgeryTokenHelper.GetRequestDataAsync(client, "/Admin/Visits", TestContext.Current.CancellationToken);
        var formData = new Dictionary<string, string> {
            ["__RequestVerificationToken"] = antiForgeryData.RequestToken
        };
        var request = new HttpRequestMessage(HttpMethod.Post, $"/Admin/MarkReimbursed/{visitId}") {
            Content = new FormUrlEncodedContent(formData)
        };
        request.Headers.Add("Cookie", antiForgeryData.CookieHeader);

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        await using var context = Fixture.CreateDbContext();
        var visit = await context.Visits.FindAsync([visitId], TestContext.Current.CancellationToken);
        visit.ShouldNotBeNull();
        visit.IsSupplierReimbursed.ShouldBeTrue();
        visit.ReimbursedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task MarkReimbursed_TogglesReimbursement_WhenCalledTwice() {
        using var client = CreateAuthenticatedClient();
        int visitId = Fixture.SeedData.SupplierVisitBookingVisits[4].Id;

        var antiForgeryData = await AntiForgeryTokenHelper.GetRequestDataAsync(client, "/Admin/Visits", TestContext.Current.CancellationToken);
        var formData = new Dictionary<string, string> {
            ["__RequestVerificationToken"] = antiForgeryData.RequestToken
        };
        var request = new HttpRequestMessage(HttpMethod.Post, $"/Admin/MarkReimbursed/{visitId}") {
            Content = new FormUrlEncodedContent(formData)
        };
        request.Headers.Add("Cookie", antiForgeryData.CookieHeader);

        var response1 = await client.SendAsync(request, TestContext.Current.CancellationToken);
        response1.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        await using (var context = Fixture.CreateDbContext()) {
            var visit = await context.Visits.FindAsync([visitId], TestContext.Current.CancellationToken);
            visit.ShouldNotBeNull();
            visit.IsSupplierReimbursed.ShouldBeTrue();
        }

        var request2 = new HttpRequestMessage(HttpMethod.Post, $"/Admin/MarkReimbursed/{visitId}") {
            Content = new FormUrlEncodedContent(formData)
        };
        request2.Headers.Add("Cookie", antiForgeryData.CookieHeader);

        var response2 = await client.SendAsync(request2, TestContext.Current.CancellationToken);
        response2.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        await using (var context = Fixture.CreateDbContext()) {
            var visit = await context.Visits.FindAsync([visitId], TestContext.Current.CancellationToken);
            visit.ShouldNotBeNull();
            visit.IsSupplierReimbursed.ShouldBeFalse();
            visit.ReimbursedAt.ShouldBeNull();
        }
    }

    [Fact]
    public async Task MarkReimbursed_ReturnsNotFound_WhenVisitDoesNotExist() {
        using var client = CreateAuthenticatedClient();
        int nonExistentVisitId = 99999;

        var antiForgeryData = await AntiForgeryTokenHelper.GetRequestDataAsync(client, "/Admin/Visits", TestContext.Current.CancellationToken);
        var formData = new Dictionary<string, string> {
            ["__RequestVerificationToken"] = antiForgeryData.RequestToken
        };
        var request = new HttpRequestMessage(HttpMethod.Post, $"/Admin/MarkReimbursed/{nonExistentVisitId}") {
            Content = new FormUrlEncodedContent(formData)
        };
        request.Headers.Add("Cookie", antiForgeryData.CookieHeader);

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarkReimbursed_RedirectsToLogin_WhenNotAuthenticated() {
        using var client = CreateApiClient();
        int visitId = Fixture.SeedData.SupplierVisitBookingVisits[0].Id;

        var response = await client.PostAsync($"/Admin/MarkReimbursed/{visitId}", new StringContent(string.Empty), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().ShouldContain("/Admin/Login");
    }

    private HttpClient CreateAuthenticatedClient() {
        var client = CreateApiClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        return client;
    }
}
