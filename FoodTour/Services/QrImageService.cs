namespace FoodTour.Services;

public class QrImageService : IQrImageService {
    private const int QR_CODE_SIZE = 20;
    public byte[] GenerateQrForUrl(string url, QRCodeGenerator.ECCLevel ecc = QRCodeGenerator.ECCLevel.Q, int pixelsPerModule = QR_CODE_SIZE) {
        if (string.IsNullOrWhiteSpace(url)) {
            throw new ArgumentException("URL cannot be null or empty.", nameof(url));
        }
        return PngByteQRCodeHelper.GetQRCode(url, ecc, pixelsPerModule);
    }
}