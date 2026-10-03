namespace FoodTour.ITest.API.Tests;

[Collection("FoodTour API application")]
public sealed class AdminSuppliersApiTests(ApiApplicationFixture fixture) : ApiTestBase(fixture) {
    [Fact]
    public async Task Suppliers_ReturnsOk_WhenAuthenticated() {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/Admin/Suppliers", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Suppliers_ReturnsSuppliersList_WithCorrectData() {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/Admin/Suppliers", TestContext.Current.CancellationToken);
        string html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain("Suppliers");
        html.ShouldContain(Fixture.SeedData.ActiveSuppliers[0].Name);
    }

    [Fact]
    public async Task Suppliers_RedirectsToLogin_WhenNotAuthenticated() {
        using var client = CreateApiClient();

        var response = await client.GetAsync("/Admin/Suppliers", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().ShouldContain("/Admin/Login");
    }

    [Fact]
    public async Task CreateSupplier_ReturnsOk_WhenAuthenticated() {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/Admin/CreateSupplier", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateSupplier_CreatesSupplier_WhenValidDataProvided() {
        using var client = CreateAuthenticatedClient();
        var antiForgeryData = await AntiForgeryTokenHelper.GetRequestDataAsync(client, "/Admin/CreateSupplier", TestContext.Current.CancellationToken);
        string supplierName = "New Test Supplier";

        var formData = new Dictionary<string, string> {
            ["__RequestVerificationToken"] = antiForgeryData.RequestToken,
            ["Name"] = supplierName,
            ["Address"] = "123 Test Street",
            ["ContactName"] = "Test Contact",
            ["ContactEmail"] = "test@example.com",
            ["ConfirmationCode"] = "TESTCODE",
            ["OpeningTimes"] = "9am - 5pm",
            ["IsActive"] = "true"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/Admin/CreateSupplier") {
            Content = new FormUrlEncodedContent(formData)
        };
        request.Headers.Add("Cookie", antiForgeryData.CookieHeader);

        try {
            var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            response.Headers.Location?.ToString().ShouldContain("/Admin/Suppliers");

            await using var context = Fixture.CreateDbContext();
            var supplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Name == supplierName, TestContext.Current.CancellationToken);
            supplier.ShouldNotBeNull();
            supplier.Address.ShouldBe("123 Test Street");
            supplier.ContactEmail.ShouldBe("test@example.com");
        } finally {
            await using var context = Fixture.CreateDbContext();
            var supplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Name == supplierName, TestContext.Current.CancellationToken);
            if (supplier != null) {
                context.Suppliers.Remove(supplier);
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }
        }
    }

    [Fact]
    public async Task CreateSupplier_ReturnsValidationError_WhenNameMissing() {
        using var client = CreateAuthenticatedClient();
        var antiForgeryData = await AntiForgeryTokenHelper.GetRequestDataAsync(client, "/Admin/CreateSupplier", TestContext.Current.CancellationToken);

        var formData = new Dictionary<string, string> {
            ["__RequestVerificationToken"] = antiForgeryData.RequestToken,
            ["Name"] = "",
            ["Address"] = "123 Test Street",
            ["ContactName"] = "Test Contact",
            ["ContactEmail"] = "test@example.com",
            ["ConfirmationCode"] = "TESTCODE"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/Admin/CreateSupplier") {
            Content = new FormUrlEncodedContent(formData)
        };
        request.Headers.Add("Cookie", antiForgeryData.CookieHeader);

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        html.ShouldContain("validation");
    }

    [Fact]
    public async Task EditSupplier_ReturnsOk_WhenSupplierExists() {
        using var client = CreateAuthenticatedClient();
        int supplierId = Fixture.SeedData.ActiveSuppliers[3].Id;

        var response = await client.GetAsync($"/Admin/EditSupplier/{supplierId}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EditSupplier_UpdatesSupplier_WhenValidDataProvided() {
        using var client = CreateAuthenticatedClient();
        int supplierId = Fixture.SeedData.ActiveSuppliers[1].Id;
        var antiForgeryData = await AntiForgeryTokenHelper.GetRequestDataAsync(client, $"/Admin/EditSupplier/{supplierId}", TestContext.Current.CancellationToken);

        var formData = new Dictionary<string, string> {
            ["__RequestVerificationToken"] = antiForgeryData.RequestToken,
            ["Id"] = supplierId.ToString(),
            ["Name"] = "Updated Supplier Name",
            ["Address"] = "Updated Address",
            ["ContactName"] = "Updated Contact",
            ["ContactEmail"] = "updated@example.com",
            ["ConfirmationCode"] = "UPDATED",
            ["OpeningTimes"] = "10am - 6pm",
            ["IsActive"] = "true"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"/Admin/EditSupplier/{supplierId}") {
            Content = new FormUrlEncodedContent(formData)
        };
        request.Headers.Add("Cookie", antiForgeryData.CookieHeader);

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        await using var context = Fixture.CreateDbContext();
        var supplier = await context.Suppliers.FindAsync([supplierId], TestContext.Current.CancellationToken);
        supplier.ShouldNotBeNull();
        supplier.Name.ShouldBe("Updated Supplier Name");
        supplier.Address.ShouldBe("Updated Address");
        supplier.ContactEmail.ShouldBe("updated@example.com");
    }

    [Fact]
    public async Task EditSupplier_ReturnsNotFound_WhenSupplierDoesNotExist() {
        using var client = CreateAuthenticatedClient();
        int nonExistentSupplierId = 99999;

        var response = await client.GetAsync($"/Admin/EditSupplier/{nonExistentSupplierId}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DisableSupplier_SoftDeletesSupplier_WhenSupplierExists() {
        using var client = CreateAuthenticatedClient();
        int supplierId = Fixture.SeedData.ActiveSuppliers[2].Id;
        var antiForgeryData = await AntiForgeryTokenHelper.GetRequestDataAsync(client, "/Admin/Suppliers", TestContext.Current.CancellationToken);

        var formData = new Dictionary<string, string> {
            ["__RequestVerificationToken"] = antiForgeryData.RequestToken
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"/Admin/DisableSupplier/{supplierId}") {
            Content = new FormUrlEncodedContent(formData)
        };
        request.Headers.Add("Cookie", antiForgeryData.CookieHeader);

        try {
            var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            response.StatusCode.ShouldBe(HttpStatusCode.Redirect);

            await using var context = Fixture.CreateDbContext();
            var supplier = await context.Suppliers.FindAsync([supplierId], TestContext.Current.CancellationToken);
            supplier.ShouldNotBeNull();
            supplier.IsActive.ShouldBeFalse();
        } finally {
            await RestoreSupplierAsync(supplierId);
        }
    }

    [Fact]
    public async Task DisableSupplier_ReturnsNotFound_WhenSupplierDoesNotExist() {
        using var client = CreateAuthenticatedClient();
        int nonExistentSupplierId = 99999;
        var antiForgeryData = await AntiForgeryTokenHelper.GetRequestDataAsync(client, "/Admin/Suppliers", TestContext.Current.CancellationToken);

        var formData = new Dictionary<string, string> {
            ["__RequestVerificationToken"] = antiForgeryData.RequestToken
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"/Admin/DisableSupplier/{nonExistentSupplierId}") {
            Content = new FormUrlEncodedContent(formData)
        };
        request.Headers.Add("Cookie", antiForgeryData.CookieHeader);

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private HttpClient CreateAuthenticatedClient() {
        var client = CreateApiClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        return client;
    }

    private async Task RestoreSupplierAsync(int supplierId) {
        await using var context = Fixture.CreateDbContext();
        var supplier = await context.Suppliers.FindAsync([supplierId], TestContext.Current.CancellationToken);
        if (supplier is not null) {
            supplier.IsActive = true;
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
    }
}
