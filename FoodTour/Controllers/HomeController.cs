namespace FoodTour.Controllers;

public class HomeController(FoodTourContext context) : Controller {
    private const int MAX_SUPPLIER_SELECTIONS = 5;

    public async Task<IActionResult> Index() {
        var model = await CreateFoodTourViewModel();

        if (model.Suppliers.Count == 0) {
            ViewBag.NoSuppliersMessage = "No tasting experiences are currently available for the tour. Please check back later.";
        } else if (model.Suppliers.Count < MAX_SUPPLIER_SELECTIONS) {
            ViewBag.InsufficientSuppliersWarning = GetInsufficientSuppliersMessage(model.Suppliers.Count);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(DateTime? tourDate, List<int> selectedSuppliers, string customerEmail) {
        if (!tourDate.HasValue) {
            ModelState.AddModelError(nameof(FoodTourVM.TourDate), "Please select a date for your tour.");
        } else if (!IsValidTourDate(tourDate.Value)) {
            ModelState.AddModelError(nameof(FoodTourVM.TourDate), "Tours are only available Monday to Saturday between March and October, and only for future dates.");
        }
        if (string.IsNullOrWhiteSpace(customerEmail)) {
            ModelState.AddModelError(nameof(FoodTourVM.CustomerEmail), "Please enter your email address.");
        } else if (!new EmailAddressAttribute().IsValid(customerEmail)) {
            ModelState.AddModelError(nameof(FoodTourVM.CustomerEmail), "Please enter a valid email address.");
        }

        var activeSelectedSupplierIds = await context.Suppliers
            .Where(s => s.IsActive && selectedSuppliers.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        if (selectedSuppliers.Count != MAX_SUPPLIER_SELECTIONS) {
            ModelState.AddModelError(string.Empty, $"You must select exactly {MAX_SUPPLIER_SELECTIONS} tasting experiences for your tour.");
        }

        if (!ModelState.IsValid) {
            var model = await CreateFoodTourViewModel();
            model.TourDate = tourDate;
            return View(model);
        }

        // TODO: Store selectedSuppliers and tourDate (e.g., in session, database, or pass to confirmation page) before redirecting to preserve user's selection and create the tour.
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Privacy() {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private async Task<FoodTourVM> CreateFoodTourViewModel() {
        var suppliers = await context.Suppliers.Where(s => s.IsActive).ToListAsync();
        return new FoodTourVM {
            Suppliers = suppliers,
            MaxSelections = MAX_SUPPLIER_SELECTIONS
        };
    }

    private static bool HasInactiveSelections(List<int> selectedSuppliers, List<int> activeSelectedSupplierIds) {
        return selectedSuppliers.Count != activeSelectedSupplierIds.Count;
    }

    private static string GetInsufficientSuppliersMessage(int supplierCount) {
        string experienceText = supplierCount == 1 ? "tasting experience is" : "tasting experiences are";
        return $"Only {supplierCount} {experienceText} currently available. You need at least {MAX_SUPPLIER_SELECTIONS} to create a tour. Please check back later.";
    }

    private static bool IsValidTourDate(DateTime date) {
        var today = DateTime.Today;
        if (date <= today) {
            return false;
        }
        if (date.DayOfWeek == DayOfWeek.Sunday) {
            return false;
        }
        if (date.Month < 3 || date.Month > 10) {
            return false;
        }
        return true;
    }
}
