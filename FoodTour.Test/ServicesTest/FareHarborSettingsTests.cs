namespace FoodTour.Test.Services;

public class FareHarborSettingsTests {
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public void Validate_ThrowsOptionsValidationException_WhenWebhookKeyIsMissingOrBlank(string? key) {
        using var services = CreateServices(key);
        var sut = services.GetRequiredService<IStartupValidator>();

        var result = Should.Throw<OptionsValidationException>(() => sut.Validate());

        result.Failures.ShouldContain(failure => failure.Contains(nameof(FareHarborSettings.WebhookKey), StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_BindsWebhookKey_WhenConfigurationIsValid() {
        using var services = CreateServices(TestHelper.VALID_KEY);
        var sut = services.GetRequiredService<IStartupValidator>();

        sut.Validate();
        var result = services.GetRequiredService<IOptions<FareHarborSettings>>().Value;

        result.WebhookKey.ShouldBe(TestHelper.VALID_KEY);
    }

    private static ServiceProvider CreateServices(string? key) {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["FareHarbor:WebhookKey"] = key
            })
            .Build();
        var services = new ServiceCollection();
        services.AddOptions<FareHarborSettings>()
            .Bind(configuration.GetSection("FareHarbor"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var result = services.BuildServiceProvider();
        return result;
    }
}
