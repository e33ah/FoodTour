namespace FoodTour.ITest.UI.Tests.Journeys;

[Collection("FoodTour application")]
public sealed class AdminSuppliersUiTests(FoodTourApplicationFixture fixture) : UiTestBase(fixture) {
    [Fact]
    public Task ViewSuppliersList_ShowsSuppliers_WhenAuthenticated() {
        return ExecutePageTestAsync(async page => {
            var sut = new AdminSuppliersPage(page);

            await sut.GotoAsync();

            await Expect(sut.PageHeading).ToBeVisibleAsync();
            await Expect(sut.SuppliersTable).ToBeVisibleAsync();
            bool hasSupplier = await sut.HasSupplierAsync(Fixture.SeedData.ActiveSuppliers[0].Name);
            hasSupplier.ShouldBeTrue();
        });
    }

    [Fact]
    public Task AddSupplier_CreatesNewSupplier_WhenValidDataProvided() {
        return ExecutePageTestAsync(async page => {
            var suppliersPage = new AdminSuppliersPage(page);
            var createPage = new AdminCreateSupplierPage(page);
            string newSupplierName = $"Test Supplier {Guid.NewGuid()}";

            try {
                await suppliersPage.GotoAsync();
                await suppliersPage.ClickAddSupplierAsync();

                await createPage.FillFormAsync(
                    name: newSupplierName,
                    address: "123 Test Street",
                    contactName: "Test Contact",
                    contactEmail: "test@example.com",
                    confirmationCode: "TESTCODE",
                    openingTimes: "9am - 5pm",
                    isActive: true);

                await createPage.ClickSaveAsync();

                await page.WaitForURLAsync("**/Admin/Suppliers");
                bool hasSupplier = await suppliersPage.HasSupplierAsync(newSupplierName);
                hasSupplier.ShouldBeTrue();
            } finally {
                await using var context = Fixture.CreateDbContext();
                var createdSupplier = await context.Suppliers.SingleOrDefaultAsync(
                    supplier => supplier.Name == newSupplierName,
                    TestContext.Current.CancellationToken);
                if (createdSupplier != null) {
                    context.Suppliers.Remove(createdSupplier);
                    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
                }
            }
        });
    }

    [Fact]
    public Task EditSupplier_UpdatesOpeningTimes_WhenValidDataProvided() {
        return ExecutePageTestAsync(async page => {
            var suppliersPage = new AdminSuppliersPage(page);
            var editPage = new AdminEditSupplierPage(page);
            var supplier = Fixture.SeedData.ActiveSuppliers[0];
            string newOpeningTimes = "10am - 6pm";
            string? originalOpeningTimes;

            await using (var context = Fixture.CreateDbContext()) {
                var trackedSupplier = await context.Suppliers.FindAsync([supplier.Id], TestContext.Current.CancellationToken);
                trackedSupplier.ShouldNotBeNull();
                originalOpeningTimes = trackedSupplier.OpeningTimes;
            }

            try {
                await suppliersPage.GotoAsync();
                await suppliersPage.ClickEditSupplierAsync(supplier.Name);

                await editPage.UpdateOpeningTimesAsync(newOpeningTimes);
                await editPage.ClickSaveAsync();

                await page.WaitForURLAsync("**/Admin/Suppliers");

                await using var context = Fixture.CreateDbContext();
                var updatedSupplier = await context.Suppliers.FindAsync([supplier.Id], TestContext.Current.CancellationToken);
                updatedSupplier.ShouldNotBeNull();
                updatedSupplier.OpeningTimes.ShouldBe(newOpeningTimes);
            } finally {
                await using var context = Fixture.CreateDbContext();
                var trackedSupplier = await context.Suppliers.FindAsync([supplier.Id], TestContext.Current.CancellationToken);
                if (trackedSupplier != null) {
                    trackedSupplier.OpeningTimes = originalOpeningTimes;
                    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
                }
            }
        });
    }

    [Fact]
    public Task DisableSupplier_SetsSupplierInactive_WhenConfirmed() {
        return ExecutePageTestAsync(async page => {
            var sut = new AdminSuppliersPage(page);
            var editPage = new AdminEditSupplierPage(page);
            var supplier = Fixture.SeedData.ActiveSuppliers[1];
            bool originalIsActive;

            await using (var context = Fixture.CreateDbContext()) {
                var trackedSupplier = await context.Suppliers.FindAsync([supplier.Id], TestContext.Current.CancellationToken);
                trackedSupplier.ShouldNotBeNull();
                originalIsActive = trackedSupplier.IsActive;
            }

            try {
                await sut.GotoAsync();
                bool isActiveBefore = await sut.IsSupplierActiveAsync(supplier.Name);
                isActiveBefore.ShouldBeTrue();

                page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
                await sut.ClickEditSupplierAsync(supplier.Name);
                await editPage.UncheckIsActiveAsync();
                await editPage.ClickSaveAsync();

                await page.WaitForURLAsync("**/Admin/Suppliers");

                await using var context = Fixture.CreateDbContext();
                var updatedSupplier = await context.Suppliers.FindAsync([supplier.Id], TestContext.Current.CancellationToken);
                updatedSupplier.ShouldNotBeNull();
                updatedSupplier.IsActive.ShouldBeFalse();
            } finally {
                await using var context = Fixture.CreateDbContext();
                var trackedSupplier = await context.Suppliers.FindAsync([supplier.Id], TestContext.Current.CancellationToken);
                if (trackedSupplier != null) {
                    trackedSupplier.IsActive = originalIsActive;
                    await context.SaveChangesAsync(TestContext.Current.CancellationToken);
                }
            }
        });
    }

    [Fact]
    public Task NavigateToSuppliers_FromDashboard_ShowsSuppliersPage() {
        return ExecutePageTestAsync(async page => {
            var dashboardPage = new AdminDashboardPage(page);
            var suppliersPage = new AdminSuppliersPage(page);

            await dashboardPage.GotoAsync();
            await dashboardPage.SuppliersLink.ClickAsync();

            await Expect(suppliersPage.PageHeading).ToBeVisibleAsync();
        });
    }
}
