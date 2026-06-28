namespace FoodTour.Test.Controllers;

public class AdminControllerSuppliersTests : AdminControllerTestBase {
    [Fact]
    public async Task Suppliers_ReturnsView() {
        var sut = CreateController();

        var result = await sut.Suppliers();

        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task Suppliers_ReturnsAllSuppliers() {
        var context = TestHelper.CreateContextWithSuppliers(
            TestHelper.CreateSupplier(1, "Bravo"),
            TestHelper.CreateSupplier(2, "Alpha"));
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Suppliers();

        var vm = result.Model.ShouldBeOfType<AdminSuppliersVM>();
        vm.Suppliers.Count.ShouldBe(2);
        vm.Suppliers.Select(s => s.Name).ShouldBe(["Alpha", "Bravo"]);
    }

    [Fact]
    public async Task Suppliers_FiltersToActiveSuppliers_WhenActiveFilterApplied() {
        var context = TestHelper.CreateContextWithSuppliers(
            new Supplier {
                Id = 1,
                Name = "Active Supplier",
                ContactName = "Active Contact",
                ContactEmail = "active@test.com",
                ConfirmationCode = "ACTIVE",
                IsActive = true
            },
            new Supplier {
                Id = 2,
                Name = "Inactive Supplier",
                ContactName = "Inactive Contact",
                ContactEmail = "inactive@test.com",
                ConfirmationCode = "INACTIVE",
                IsActive = false
            });
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Suppliers(filter: "active");

        var vm = result.Model.ShouldBeOfType<AdminSuppliersVM>();
        vm.ActiveFilter.ShouldBe("active");
        vm.Suppliers.Select(s => s.Name).ShouldBe(["Active Supplier"]);
    }

    [Fact]
    public async Task Suppliers_FiltersToInactiveSuppliers_WhenInactiveFilterApplied() {
        var context = TestHelper.CreateContextWithSuppliers(
            new Supplier {
                Id = 1,
                Name = "Active Supplier",
                ContactName = "Active Contact",
                ContactEmail = "active@test.com",
                ConfirmationCode = "ACTIVE",
                IsActive = true
            },
            new Supplier {
                Id = 2,
                Name = "Inactive Supplier",
                ContactName = "Inactive Contact",
                ContactEmail = "inactive@test.com",
                ConfirmationCode = "INACTIVE",
                IsActive = false
            });
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Suppliers(filter: "inactive");

        var vm = result.Model.ShouldBeOfType<AdminSuppliersVM>();
        vm.ActiveFilter.ShouldBe("inactive");
        vm.Suppliers.Select(s => s.Name).ShouldBe(["Inactive Supplier"]);
    }

    [Fact]
    public async Task Suppliers_SortsByContactName_WhenContactSortApplied() {
        var context = TestHelper.CreateContextWithSuppliers(
            new Supplier {
                Id = 1,
                Name = "Zulu Supplier",
                ContactName = "Zed",
                ContactEmail = "zed@test.com",
                ConfirmationCode = "ZED",
                IsActive = true
            },
            new Supplier {
                Id = 2,
                Name = "Alpha Supplier",
                ContactName = "Amy",
                ContactEmail = "amy@test.com",
                ConfirmationCode = "AMY",
                IsActive = true
            });
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Suppliers(sort: AdminSuppliersVM.SORT_CONTACT);

        var vm = result.Model.ShouldBeOfType<AdminSuppliersVM>();
        vm.SortColumn.ShouldBe(AdminSuppliersVM.SORT_CONTACT);
        vm.SortDescending.ShouldBeFalse();
        vm.Suppliers.Select(s => s.ContactName).ShouldBe(["Amy", "Zed"]);
    }

    [Fact]
    public async Task Suppliers_SortsByActiveStatus_WhenActiveSortApplied() {
        var context = TestHelper.CreateContextWithSuppliers(
            new Supplier {
                Id = 1,
                Name = "Active Supplier",
                ContactName = "Active Contact",
                ContactEmail = "active@test.com",
                ConfirmationCode = "ACTIVE",
                IsActive = true
            },
            new Supplier {
                Id = 2,
                Name = "Inactive Supplier",
                ContactName = "Inactive Contact",
                ContactEmail = "inactive@test.com",
                ConfirmationCode = "INACTIVE",
                IsActive = false
            });
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Suppliers(sort: AdminSuppliersVM.SORT_ACTIVE);

        var vm = result.Model.ShouldBeOfType<AdminSuppliersVM>();
        vm.SortColumn.ShouldBe(AdminSuppliersVM.SORT_ACTIVE);
        vm.SortDescending.ShouldBeTrue();
        vm.Suppliers.Select(s => s.Name).ShouldBe(["Active Supplier", "Inactive Supplier"]);
    }

    [Fact]
    public async Task CreateSupplier_Get_ReturnsView() {
        var sut = CreateController();

        var result = sut.CreateSupplier();

        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task CreateSupplier_CreatesSupplier_WhenModelStateValid() {
        var context = TestHelper.CreateInMemoryContext();
        var sut = CreateController(context: context);
        var model = new AdminSupplierFormVM {
            Name = "New Supplier",
            ContactName = "Contact",
            ContactEmail = "contact@test.com",
            ConfirmationCode = "TOUR",
            IsActive = true,
            EmailNotificationsEnabled = true,
            Address = "1 Street",
            OpeningTimes = "9am - 5pm"
        };

        var result = await sut.CreateSupplier(model);

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe(nameof(AdminController.Suppliers));

        var suppliers = await context.Suppliers.OrderBy(s => s.Id).ToListAsync(CancellationToken);
        suppliers.Count.ShouldBe(1);
        suppliers[0].Name.ShouldBe("New Supplier");
        suppliers[0].ContactEmail.ShouldBe("contact@test.com");
        suppliers[0].EmailNotificationsEnabled.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateSupplier_ReturnsView_WhenModelStateInvalid() {
        var context = TestHelper.CreateInMemoryContext();
        var sut = CreateController(context: context);
        var model = new AdminSupplierFormVM();
        sut.ModelState.AddModelError("Name", "Required");

        var result = await sut.CreateSupplier(model);

        result.ShouldBeOfType<ViewResult>();
        context.Suppliers.Count().ShouldBe(0);
    }

    [Fact]
    public async Task EditSupplier_Get_ReturnsNotFound_WhenSupplierMissing() {
        var context = TestHelper.CreateInMemoryContext();
        var sut = CreateController(context: context);

        var result = await sut.EditSupplier(999);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task EditSupplier_Get_ReturnsView_WithModel() {
        var context = TestHelper.CreateContextWithSuppliers(TestHelper.CreateSupplier(1, "Alpha"));
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.EditSupplier(1);

        var vm = result.Model.ShouldBeOfType<AdminSupplierFormVM>();
        vm.Name.ShouldBe("Alpha");
        vm.Id.ShouldBe(1);
    }

    [Fact]
    public async Task EditSupplier_ReturnsBadRequest_WhenIdDoesNotMatchModelId() {
        var sut = CreateController();

        int routeId = 1;
        var model = new AdminSupplierFormVM { Id = 2 };

        var result = await sut.EditSupplier(routeId, model);

        result.ShouldBeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task EditSupplier_UpdatesSupplier_WhenModelStateValid() {
        var supplier = TestHelper.CreateSupplier(1, "Old");
        var context = TestHelper.CreateContextWithSuppliers(supplier);
        var sut = CreateController(context: context);
        var model = AdminSupplierFormVM.FromSupplier(supplier);
        model.Name = "Updated";
        model.IsActive = false;

        var result = await sut.EditSupplier(supplier.Id, model);

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe(nameof(AdminController.Suppliers));

        var updated = await context.Suppliers.FindAsync([supplier.Id], CancellationToken);
        updated!.Name.ShouldBe("Updated");
        updated.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task EditSupplier_ReturnsView_WhenModelStateInvalid() {
        var supplier = TestHelper.CreateSupplier(1, "Old");
        var context = TestHelper.CreateContextWithSuppliers(supplier);
        var sut = CreateController(context: context);
        var model = AdminSupplierFormVM.FromSupplier(supplier);
        sut.ModelState.AddModelError("Name", "Required");

        var result = await sut.EditSupplier(supplier.Id, model);

        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task DeleteSupplier_ReturnsNotFound_WhenSupplierMissing() {
        var context = TestHelper.CreateInMemoryContext();
        var sut = CreateController(context: context);

        var result = await sut.DisableSupplier(999);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteSupplier_SetsIsActiveFalse_WhenSupplierExists() {
        var supplier = TestHelper.CreateSupplier(1, "Active", isActive: true);
        var context = TestHelper.CreateContextWithSuppliers(supplier);
        var sut = CreateController(context: context);

        var result = await sut.DisableSupplier(supplier.Id);

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe(nameof(AdminController.Suppliers));

        var updated = await context.Suppliers.FindAsync([supplier.Id], CancellationToken);
        updated!.IsActive.ShouldBeFalse();
    }
}
