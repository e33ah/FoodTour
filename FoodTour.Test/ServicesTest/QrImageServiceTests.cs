namespace FoodTour.Test.ServicesTest;

public class QrImageServiceTests {
    private readonly QrImageService sut = new();

    [Fact]
    public void GenerateQrForUrl_ShouldReturnByteArray_WhenValidUrlProvided() {
        string url = "https://example.com";

        byte[] result = sut.GenerateQrForUrl(url);

        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void GenerateQrForUrl_ShouldThrowArgumentNullException_WhenUrlIsNull() {
        string url = null!;

        Should.Throw<ArgumentException>(() => sut.GenerateQrForUrl(url));
    }

    [Fact]
    public void GenerateQrForUrl_ShouldThrowArgumentException_WhenUrlIsEmpty() {
        string url = string.Empty;

        Should.Throw<ArgumentException>(() => sut.GenerateQrForUrl(url));
    }

    [Theory]
    [InlineData(QRCodeGenerator.ECCLevel.L)]
    [InlineData(QRCodeGenerator.ECCLevel.M)]
    [InlineData(QRCodeGenerator.ECCLevel.Q)]
    [InlineData(QRCodeGenerator.ECCLevel.H)]
    public void GenerateQrForUrl_ShouldGenerateQrCode_WithDifferentEccLevels(QRCodeGenerator.ECCLevel eccLevel) {
        string url = "https://example.com";

        byte[] result = sut.GenerateQrForUrl(url, eccLevel);

        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    public void GenerateQrForUrl_ShouldGenerateQrCode_WithDifferentPixelSizes(int pixelsPerModule) {
        string url = "https://example.com";

        byte[] result = sut.GenerateQrForUrl(url, QRCodeGenerator.ECCLevel.Q, pixelsPerModule);

        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);
    }
}
