namespace FoodTour.Test;

public static class TestHelper {
    public static FoodTourContext CreateInMemoryContext() {
        var options = new DbContextOptionsBuilder<FoodTourContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new FoodTourContext(options);
    }

    public static Supplier CreateSupplier(int id, string name, bool isActive = true, string? address = null) {
        return new Supplier {
            Id = id,
            Name = name,
            ContactName = "Mrs Test",
            ContactEmail = $"contact{id}@test.com",
            IsActive = isActive,
            Address = address
        };
    }

    public static FoodTourContext CreateContextWithSuppliers(params Supplier[] suppliers) {
        var context = CreateInMemoryContext();
        context.Suppliers.AddRange(suppliers);
        context.SaveChanges();
        return context;
    }

    public static FoodTourContext CreateContextWithActiveSuppliersCount(int count) {
        var suppliers = Enumerable.Range(1, count)
            .Select(i => CreateSupplier(i, $"Supplier {i}"))
            .ToArray();
        return CreateContextWithSuppliers(suppliers);
    }

    public static List<int> CreateSupplierIdList(int count) {
        return [.. Enumerable.Range(1, count)];
    }
}
