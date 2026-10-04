namespace FoodTour.Test.Models;

public class AdminSuppliersVMSortTests {
    [Theory]
    [InlineData(AdminSuppliersVM.SORT_NAME, true, "asc")]
    [InlineData(AdminSuppliersVM.SORT_NAME, false, "desc")]
    [InlineData(AdminSuppliersVM.SORT_ACTIVE, true, "asc")]
    [InlineData(AdminSuppliersVM.SORT_ACTIVE, false, "desc")]
    public void NextDirFor_ReturnsToggledDirection_WhenColumnIsCurrentlySorted(string sortColumn, bool sortDescending, string expectedDirection) {
        var sut = new AdminSuppliersVM {
            SortColumn = sortColumn,
            SortDescending = sortDescending
        };

        string result = sut.NextDirFor(sortColumn);

        result.ShouldBe(expectedDirection);
    }

    [Theory]
    [InlineData(AdminSuppliersVM.SORT_NAME, false, "asc")]
    [InlineData(AdminSuppliersVM.SORT_ACTIVE, false, "desc")]
    public void NextDirFor_ReturnsExpectedDirection_WhenColumnNotCurrentlySorted(string sortColumn, bool sortDescending, string expectedDirection) {
        var sut = new AdminSuppliersVM {
            SortColumn = AdminSuppliersVM.SORT_CONTACT,
            SortDescending = sortDescending
        };

        string result = sut.NextDirFor(sortColumn);

        result.ShouldBe(expectedDirection);
    }

    [Fact]
    public void IsSortedBy_ReturnsTrue_WhenColumnMatchesIgnoringCase() {
        var sut = new AdminSuppliersVM { SortColumn = AdminSuppliersVM.SORT_EMAILS };

        bool result = sut.IsSortedBy("EMAILS");

        result.ShouldBeTrue();
    }
}
