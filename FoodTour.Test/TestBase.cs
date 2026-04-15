namespace FoodTour.Test;

public abstract class TestBase {
    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;
}
