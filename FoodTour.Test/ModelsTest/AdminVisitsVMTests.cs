namespace FoodTour.Test.Models;

public class AdminVisitsVMTests {
    [Fact]
    public void Constructor_SortsDescending_WhenCreatedWithDefaults() {
        var sut = new AdminVisitsVM();

        sut.SortDescending.ShouldBeTrue();
    }

    private static AdminVisitsVM BuildVM(int totalCount, int currentPage, int pageSize = 25) =>
        new() { TotalCount = totalCount, CurrentPage = currentPage, PageSize = pageSize };

    private static IReadOnlyList<int?> PageNumbers(AdminVisitsVM vm) =>
        [.. vm.PageNumbers.Select(entry => entry.PageNumber)];

    [Fact]
    public void TotalPages_ReturnsOne_WhenPageSizeIsZero() {
        var sut = BuildVM(totalCount: 100, currentPage: 1, pageSize: 0);

        sut.TotalPages.ShouldBe(1);
    }

    [Fact]
    public void TotalPages_RoundsUp_WhenLastPageIsPartial() {
        var sut = BuildVM(totalCount: 51, currentPage: 1);

        sut.TotalPages.ShouldBe(3);
    }

    [Fact]
    public void HasPreviousPage_ReturnsFalse_OnFirstPage() {
        var sut = BuildVM(totalCount: 100, currentPage: 1);

        sut.HasPreviousPage.ShouldBeFalse();
    }

    [Fact]
    public void HasNextPage_ReturnsFalse_OnLastPage() {
        var sut = BuildVM(totalCount: 50, currentPage: 2);

        sut.HasNextPage.ShouldBeFalse();
    }

    [Fact]
    public void PageNumbers_ReturnsAllPages_WhenTotalPagesWithinWindow() {
        var sut = BuildVM(totalCount: 50, currentPage: 1);

        PageNumbers(sut).ShouldBe([1, 2]);
    }

    [Fact]
    public void PageNumbers_ReturnsWindowAroundCurrentPage_WithEllipsis() {
        var sut = BuildVM(totalCount: 250, currentPage: 5);

        PageNumbers(sut).ShouldBe([1, null, 3, 4, 5, 6, 7, null, 10]);
    }

    [Fact]
    public void PageNumbers_ReturnsOnePage_WhenPageSizeIsZero() {
        var sut = BuildVM(totalCount: 100, currentPage: 1, pageSize: 0);

        PageNumbers(sut).ShouldBe([1]);
    }

    [Theory]
    [InlineData(AdminVisitsVM.SORT_DATE, true, "asc")]
    [InlineData(AdminVisitsVM.SORT_DATE, false, "desc")]
    [InlineData(AdminVisitsVM.SORT_CUSTOMER, true, "asc")]
    [InlineData(AdminVisitsVM.SORT_CUSTOMER, false, "desc")]
    public void NextDirFor_ReturnsExpectedDirection(string sortColumn, bool sortDescending, string expectedDirection) {
        var sut = new AdminVisitsVM {
            SortColumn = sortColumn,
            SortDescending = sortDescending
        };

        string result = sut.NextDirFor(sortColumn);

        result.ShouldBe(expectedDirection);
    }

    [Theory]
    [InlineData(AdminVisitsVM.SORT_DATE, "desc")]
    [InlineData(AdminVisitsVM.SORT_REF, "desc")]
    [InlineData(AdminVisitsVM.SORT_CUSTOMER, "asc")]
    [InlineData(AdminVisitsVM.SORT_SUPPLIER, "asc")]
    [InlineData(AdminVisitsVM.SORT_GUESTS, "asc")]
    public void NextDirFor_ReturnsDefaultDirection_WhenColumnIsNotCurrentlySorted(string column, string expectedDirection) {
        var sut = new AdminVisitsVM {
            SortColumn = AdminVisitsVM.SORT_CUSTOMER,
            SortDescending = true
        };

        string result = sut.NextDirFor(column);

        result.ShouldBe(expectedDirection);
    }

    [Fact]
    public void NextDirFor_ReturnsToggledDirection_WhenColumnMatchesIgnoringCase() {
        var sut = new AdminVisitsVM {
            SortColumn = AdminVisitsVM.SORT_DATE,
            SortDescending = true
        };

        string result = sut.NextDirFor("DATE");

        result.ShouldBe("asc");
    }
}
