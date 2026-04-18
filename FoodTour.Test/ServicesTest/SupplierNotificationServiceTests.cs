namespace FoodTour.Test.Services;

public class SupplierNotificationServiceTests {
    [Fact]
    public async Task NotifySupplierVisitRecordedAsync_DoesNotSendEmail_WhenEmailNotificationsAreDisabled() {
        var booking = TestHelper.CreateValidBooking();
        var supplier = TestHelper.CreateSupplier(1);
        supplier.EmailNotificationsEnabled = false;
        var visit = TestHelper.CreateVisit();
        var (sut, emailSenderMock, _) = TestHelper.CreateSupplierNotificationService();

        await sut.NotifySupplierVisitRecordedAsync(booking, supplier, visit, TestHelper.SUPPORT_URL);

        emailSenderMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task NotifySupplierVisitRecordedAsync_SendsEmail_WhenEmailNotificationsAreEnabled() {
        var booking = TestHelper.CreateValidBooking();
        var supplier = TestHelper.CreateSupplier(1);
        supplier.EmailNotificationsEnabled = true;
        var visit = TestHelper.CreateVisit();
        var (sut, emailSenderMock, templateRendererMock) = TestHelper.CreateSupplierNotificationService();
        templateRendererMock.Setup(x => x.RenderTemplateAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync((string path, object model) => $"<html>Email body for {supplier.Name}</html>");

        await sut.NotifySupplierVisitRecordedAsync(booking, supplier, visit, TestHelper.SUPPORT_URL);

        emailSenderMock.Verify(x => x.SendEmailAsync(
            supplier.ContactEmail,
            It.Is<string>(subject => subject.Contains(TestHelper.formattedTourDate)),
            It.Is<string>(body => body.Contains(supplier.Name))
        ), Times.Once);
    }

    [Fact]
    public async Task NotifySupplierVisitRecordedAsync_SendsEmailToEachRecipient_WhenMultipleEmailAddressesConfigured() {
        var booking = TestHelper.CreateValidBooking();
        var supplier = TestHelper.CreateSupplier(1);
        supplier.EmailNotificationsEnabled = true;
        supplier.ContactEmail = "first@test.com; second@test.com ;third@test.com";
        var visit = TestHelper.CreateVisit();
        var (sut, emailSenderMock, templateRendererMock) = TestHelper.CreateSupplierNotificationService();
        templateRendererMock.Setup(x => x.RenderTemplateAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync((string path, object model) => $"<html>Email body for {supplier.Name}</html>");

        await sut.NotifySupplierVisitRecordedAsync(booking, supplier, visit, TestHelper.SUPPORT_URL);

        emailSenderMock.Verify(x => x.SendEmailAsync(
            "first@test.com",
            It.Is<string>(subject => subject.Contains(TestHelper.formattedTourDate)),
            It.Is<string>(body => body.Contains(supplier.Name))
        ), Times.Once);
        emailSenderMock.Verify(x => x.SendEmailAsync(
            "second@test.com",
            It.Is<string>(subject => subject.Contains(TestHelper.formattedTourDate)),
            It.Is<string>(body => body.Contains(supplier.Name))
        ), Times.Once);
        emailSenderMock.Verify(x => x.SendEmailAsync(
            "third@test.com",
            It.Is<string>(subject => subject.Contains(TestHelper.formattedTourDate)),
            It.Is<string>(body => body.Contains(supplier.Name))
        ), Times.Once);
    }
}
