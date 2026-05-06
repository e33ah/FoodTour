namespace FoodTour.ITest.API.Tests.Helpers;

internal sealed class RecordVisitRequest {
    public required Guid QrToken { get; init; }

    public required int SelectedSupplierId { get; init; }

    public required string SupplierCode { get; init; }

    public int TastingExperiencesClaimed { get; init; } = 1;
}

internal static class VisitsApiRequestFactory {
    public static string CreateSupplierVisitPath(Guid qrToken) {
        string result = $"/Home/SupplierVisit?qrToken={qrToken}";
        return result;
    }

    public static StringContent CreateRecordVisitContent(RecordVisitRequest request) {
        string json = $$"""
            {
                "qrToken": "{{request.QrToken}}",
                "selectedSupplierId": {{request.SelectedSupplierId}},
                "supplierCode": "{{request.SupplierCode}}",
                "tastingExperiencesClaimed": {{request.TastingExperiencesClaimed}}
            }
            """;

        StringContent result = new(json, Encoding.UTF8, "application/json");
        return result;
    }
}
