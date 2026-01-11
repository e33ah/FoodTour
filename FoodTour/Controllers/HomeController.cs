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
    public async Task<IActionResult> Index(List<int> selectedSuppliers) {
        var activeSelectedSupplierIds = await context.Suppliers
            .Where(s => s.IsActive && selectedSuppliers.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        if (selectedSuppliers.Count != MAX_SUPPLIER_SELECTIONS) {
            ModelState.AddModelError(string.Empty, $"You must select exactly {MAX_SUPPLIER_SELECTIONS} tasting experiences for your tour.");
            return View(await CreateFoodTourViewModel());
        }

        // TODO: Store selectedSuppliers (e.g., in session, database, or pass to confirmation page) before redirecting to preserve user's selection and create the tour.
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
}
