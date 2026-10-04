namespace FoodTour.Test.Models;

public class AdminBookingsVMTests {
    [Fact]
    public void TotalPages_ReturnsOne_WhenPageSizeIsZero() {
        var sut = new AdminBookingsVM { TotalCount = 100, PageSize = 0 };

        int result = sut.TotalPages;

        result.ShouldBe(1);
    }

    [Fact]
    public void Constructor_SortsDescending_WhenCreatedWithDefaults() {
        var sut = new AdminBookingsVM();

        sut.SortDescending.ShouldBeTrue();
    }

    private static AdminBookingsVM BuildVM(int totalCount, int currentPage, int pageSize = 25) =>
        new() { TotalCount = totalCount, CurrentPage = currentPage, PageSize = pageSize };

    private static IReadOnlyList<int?> PageNums(AdminBookingsVM vm) =>
        [.. vm.PageNumbers.Select(e => e.PageNumber)];

    [Fact]
    public void PageNumbers_ReturnsAllPages_WhenTotalPagesWithinWindow() {
        var sut = BuildVM(totalCount: 50, currentPage: 1);

        PageNums(sut).ShouldBe([1, 2]);
    }

    [Fact]
    public void PageNumbers_ReturnsWindowAroundCurrentPage_WithEllipsis() {
        var sut = BuildVM(totalCount: 250, currentPage: 5);

        // Pages: 1 … 3 4 [5] 6 7 … 10
        PageNums(sut).ShouldBe([1, null, 3, 4, 5, 6, 7, null, 10]);
    }

    [Fact]
    public void PageNumbers_NoLeadingEllipsis_WhenWindowTouchesFirstPage() {
        var sut = BuildVM(totalCount: 250, currentPage: 3);

        // Pages: 1 2 [3] 4 5 … 10
        PageNums(sut).ShouldBe([1, 2, 3, 4, 5, null, 10]);
    }

    [Fact]
    public void PageNumbers_NoTrailingEllipsis_WhenWindowTouchesLastPage() {
        var sut = BuildVM(totalCount: 250, currentPage: 8);

        // Pages: 1 … 6 7 [8] 9 10
        PageNums(sut).ShouldBe([1, null, 6, 7, 8, 9, 10]);
    }

    [Fact]
    public void PageNumbers_ReturnsAllPages_WhenOnlyOnePage() {
        var sut = BuildVM(totalCount: 10, currentPage: 1);

        PageNums(sut).ShouldBe([1]);
    }

    [Fact]
    public void PageNumbers_ReturnsNoEllipsis_WhenAllPagesVisible() {
        var sut = BuildVM(totalCount: 125, currentPage: 3);

        // 5 pages total, window of ±2 around page 3 covers all
        sut.PageNumbers.ShouldNotContain(e => e.IsEllipsis);
    }
}

public class AdminBookingsVMSortTests {
    [Fact]
    public void NextDirFor_ReturnsDesc_WhenRefColumnClickedWhileNotActive() {
        var sut = new AdminBookingsVM { SortColumn = AdminBookingsVM.SORT_DATE, SortDescending = true };

        sut.NextDirFor(AdminBookingsVM.SORT_REF).ShouldBe("desc");
    }

    [Fact]
    public void NextDirFor_ReturnsDesc_WhenDateColumnClickedWhileNotActive() {
        var sut = new AdminBookingsVM { SortColumn = AdminBookingsVM.SORT_GUESTS, SortDescending = false };

        sut.NextDirFor(AdminBookingsVM.SORT_DATE).ShouldBe("desc");
    }

    [Fact]
    public void NextDirFor_ReturnsAsc_WhenNonDateColumnClickedWhileNotActive() {
        var sut = new AdminBookingsVM { SortColumn = AdminBookingsVM.SORT_DATE, SortDescending = true };

        sut.NextDirFor(AdminBookingsVM.SORT_CUSTOMER).ShouldBe("asc");
    }

    [Fact]
    public void NextDirFor_ReturnsAsc_WhenActiveDateColumnIsDescending() {
        var sut = new AdminBookingsVM { SortColumn = AdminBookingsVM.SORT_DATE, SortDescending = true };

        sut.NextDirFor(AdminBookingsVM.SORT_DATE).ShouldBe("asc");
    }

    [Fact]
    public void NextDirFor_ReturnsDesc_WhenActiveDateColumnIsAscending() {
        var sut = new AdminBookingsVM { SortColumn = AdminBookingsVM.SORT_DATE, SortDescending = false };

        sut.NextDirFor(AdminBookingsVM.SORT_DATE).ShouldBe("desc");
    }

    [Fact]
    public void NextDirFor_IsConsistentWithIsSortedBy_WhenColIsMixedCase() {
        var sut = new AdminBookingsVM { SortColumn = AdminBookingsVM.SORT_DATE, SortDescending = true };

        sut.NextDirFor("DATE").ShouldBe("asc");
    }

    [Fact]
    public void NextDirFor_DefaultsToDesc_WhenRefColumnMixedCaseAndNotActive() {
        var sut = new AdminBookingsVM { SortColumn = AdminBookingsVM.SORT_DATE, SortDescending = true };

        sut.NextDirFor("REF").ShouldBe("desc");
    }
}

public class PageEntryTests {
    [Fact]
    public void For_SetsPageNumber() {
        var result = PageEntry.For(3);

        result.PageNumber.ShouldBe(3);
    }

    [Fact]
    public void For_IsNotEllipsis() {
        var result = PageEntry.For(3);

        result.IsEllipsis.ShouldBeFalse();
    }

    [Fact]
    public void Ellipsis_IsEllipsis() {
        var result = PageEntry.Ellipsis;

        result.IsEllipsis.ShouldBeTrue();
    }

    [Fact]
    public void Ellipsis_HasNullPageNumber() {
        var result = PageEntry.Ellipsis;

        result.PageNumber.ShouldBeNull();
    }
}
