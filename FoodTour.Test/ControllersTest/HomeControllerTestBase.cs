namespace FoodTour.Test.Controllers;

public abstract class HomeControllerTestBase {
    protected static HomeController CreateController() {
        return new HomeController(TestHelper.CreateInMemoryContext());
    }

    protected static HomeController CreateControllerWithTempData(FoodTourContext context) {
        return new HomeController(context) {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }
}