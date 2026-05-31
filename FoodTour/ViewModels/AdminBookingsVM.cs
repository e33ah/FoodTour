namespace FoodTour.ViewModels;

public class AdminBookingsVM {
    private const int PAGE_WINDOW = 2;

    public IReadOnlyList<Booking> Bookings { get; set; } = [];
    public string? ActiveFilter { get; set; }
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// Windowed page entries for pagination. Always includes page 1 and the last page;
    /// shows CurrentPage ± <see cref="PAGE_WINDOW"/>, with ellipsis entries for gaps.
    /// </summary>
    internal IReadOnlyList<PageEntry> PageNumbers {
        get {
            var visible = Enumerable.Range(1, TotalPages)
                .Where(i => i == 1 || i == TotalPages || Math.Abs(i - CurrentPage) <= PAGE_WINDOW)
                .ToList();

            var result = new List<PageEntry>();
            for (int idx = 0; idx < visible.Count; idx++) {
                if (idx > 0 && visible[idx] - visible[idx - 1] > 1) {
                    result.Add(PageEntry.Ellipsis);
                }
                result.Add(PageEntry.For(visible[idx]));
            }
            return result;
        }
    }
}

internal record PageEntry(int? PageNumber) {
    public bool IsEllipsis => PageNumber is null;
    public static PageEntry Ellipsis { get; } = new PageEntry((int?)null);
    public static PageEntry For(int page) => new(page);
}
