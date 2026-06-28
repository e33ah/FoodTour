namespace FoodTour.Models;

public class AdminSuppliersVM {
    public const string SORT_NAME = "name";
    public const string SORT_CONTACT = "contact";
    public const string SORT_ACTIVE = "active";
    public const string SORT_EMAILS = "emails";

    public IReadOnlyList<Supplier> Suppliers { get; set; } = [];
    public string? ActiveFilter { get; set; }
    public string SortColumn { get; set; } = SORT_NAME;
    public bool SortDescending { get; set; }

    public bool IsSortedBy(string col) =>
        string.Equals(SortColumn, col, StringComparison.OrdinalIgnoreCase);

    public string NextDirFor(string col) {
        string normalisedCol = col.ToLowerInvariant();
        bool isDescendingColumn = normalisedCol is SORT_ACTIVE or SORT_EMAILS;
        return IsSortedBy(normalisedCol) ? (SortDescending ? "asc" : "desc") : (isDescendingColumn ? "desc" : "asc");
    }
}
