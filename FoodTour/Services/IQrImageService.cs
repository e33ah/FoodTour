namespace FoodTour.Services;

public interface IQrImageService {
    byte[] GenerateQrForUrl(string url, QRCoder.QRCodeGenerator.ECCLevel ecc, int pixelsPerModule);
}