namespace FoodTour.ITest.Shared.Http;

public sealed record AntiForgeryRequestData(string RequestToken, string CookieHeader);
