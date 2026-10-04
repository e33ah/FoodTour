namespace FoodTour.Test.Services;

public class SupplierNotificationServiceTests {
    [Fact]
    public async Task NotifySupplierVisitRecordedAsync_DoesNotSendEmail_WhenEmailNotificationsAreDisabled() {
        var booking = TestHelper.CreateValidBooking();
        var supplier = TestHelper.CreateSupplier(1);
        supplier.EmailNotificationsEnabled = false;
        var visit = TestHelper.CreateVisit(isClaimed: true);
        var (sut, emailSenderMock, _) = TestHelper.CreateSupplierNotificationService();

        await sut.NotifySupplierVisitRecordedAsync(booking, supplier, visit, TestHelper.SUPPORT_URL);

        emailSenderMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task NotifySupplierVisitRecordedAsync_SendsEmail_WhenEmailNotificationsAreEnabled() {
        var booking = TestHelper.CreateValidBooking();
        var supplier = TestHelper.CreateSupplier(1);
        supplier.EmailNotificationsEnabled = true;
        var visit = TestHelper.CreateVisit(isClaimed: true);
        var (sut, emailSenderMock, templateRendererMock) = TestHelper.CreateSupplierNotificationService();
        string expectedBody = $"<html>Email body for {supplier.Name}</html>";
        templateRendererMock.Setup(x => x.RenderTemplateAsync("Emails/SupplierVisitRecorded", It.IsAny<object>()))
            .ReturnsAsync(expectedBody);

        await sut.NotifySupplierVisitRecordedAsync(booking, supplier, visit, TestHelper.SUPPORT_URL);

        templateRendererMock.Verify(x => x.RenderTemplateAsync("Emails/SupplierVisitRecorded", It.IsAny<object>()), Times.Once);
        emailSenderMock.Verify(x => x.SendEmailAsync(
            supplier.ContactEmail,
            It.Is<string>(subject => subject.Contains(TestHelper.formattedTourDate)),
            expectedBody
        ), Times.Once);
    }

    [Fact]
    public async Task NotifySupplierVisitRecordedAsync_SendsEmailToEachRecipient_WhenMultipleEmailAddressesConfigured() {
        var booking = TestHelper.CreateValidBooking();
        var supplier = TestHelper.CreateSupplier(1);
        supplier.EmailNotificationsEnabled = true;
        supplier.ContactEmail = "first@test.com; second@test.com ;third@test.com";
        var visit = TestHelper.CreateVisit(isClaimed: true);
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

    [Fact]
    public async Task NotifySupplierVisitRecordedAsync_ThrowsException_WhenSupplierContactEmailIsNotConfigured() {
        var booking = TestHelper.CreateValidBooking();
        var supplier = TestHelper.CreateSupplier(1);
        supplier.EmailNotificationsEnabled = true;
        supplier.ContactEmail = null!; // Not configured
        var visit = TestHelper.CreateVisit(isClaimed: true);

        var (sut, _, _) = TestHelper.CreateSupplierNotificationService();

        var result = await Should.ThrowAsync<InvalidOperationException>(() =>
            sut.NotifySupplierVisitRecordedAsync(booking, supplier, visit, TestHelper.SUPPORT_URL));

        result.Message.ShouldBe("Supplier.ContactEmail is not configured.");
    }

    [Fact]
    public async Task NotifySupplierVisitRecordedAsync_ThrowsException_WhenVisitClaimedAtIsNull() {
        var booking = TestHelper.CreateValidBooking();
        var supplier = TestHelper.CreateSupplier(1);
        supplier.EmailNotificationsEnabled = true;
        var visit = TestHelper.CreateVisit(isClaimed: false); // ClaimedAt is null

        var (sut, _, _) = TestHelper.CreateSupplierNotificationService();

        var result = await Should.ThrowAsync<InvalidOperationException>(() =>
            sut.NotifySupplierVisitRecordedAsync(booking, supplier, visit, TestHelper.SUPPORT_URL));

        result.Message.ShouldBe("Visit.ClaimedAt is required.");
    }
    [Fact]
    public async Task NotifySupplierVisitRecordedAsync_ThrowsException_WhenSupportUrlIsNullOrEmpty() {
        var booking = TestHelper.CreateValidBooking();
        var supplier = TestHelper.CreateSupplier(1);
        supplier.EmailNotificationsEnabled = true;
        var visit = TestHelper.CreateVisit(isClaimed: true);

        var (sut, _, _) = TestHelper.CreateSupplierNotificationService();

        var result = await Should.ThrowAsync<InvalidOperationException>(() =>
            sut.NotifySupplierVisitRecordedAsync(booking, supplier, visit, null!));

        result.Message.ShouldBe("Support URL is required.");
    }

    [Fact]
    public async Task NotifySupplierVisitRecordedAsync_ThrowsException_WhenSupplierNameIsNullOrEmpty() {
        var booking = TestHelper.CreateValidBooking();
        var supplier = TestHelper.CreateSupplier(1);
        supplier.EmailNotificationsEnabled = true;
        supplier.Name = null!; // Name is null
        var visit = TestHelper.CreateVisit(isClaimed: true);
        var (sut, _, _) = TestHelper.CreateSupplierNotificationService();

        var result = await Should.ThrowAsync<InvalidOperationException>(() =>
            sut.NotifySupplierVisitRecordedAsync(booking, supplier, visit, TestHelper.SUPPORT_URL));

        result.Message.ShouldBe("Supplier.Name is required.");
    }

    [Fact]
    public async Task NotifySupplierVisitRecordedAsync_ThrowsException_WhenSupplierContactEmailIsEmpty() {
        var booking = TestHelper.CreateValidBooking();
        var supplier = TestHelper.CreateSupplier(1);
        supplier.EmailNotificationsEnabled = true;
        supplier.ContactEmail = ""; // Empty
        var visit = TestHelper.CreateVisit(isClaimed: true);
        
        var (sut, _, _) = TestHelper.CreateSupplierNotificationService();

        var result = await Should.ThrowAsync<InvalidOperationException>(() =>
            sut.NotifySupplierVisitRecordedAsync(booking, supplier, visit, TestHelper.SUPPORT_URL));

        result.Message.ShouldBe("Supplier.ContactEmail is not configured.");
    }
    [Fact]
    public async Task NotifySupplierVisitRecordedAsync_ThrowsException_WhenSupplierContactEmailIsEmptyList() {
        var booking = TestHelper.CreateValidBooking();
        var supplier = TestHelper.CreateSupplier(1);
        supplier.EmailNotificationsEnabled = true;
        supplier.ContactEmail = ";;"; // Empty
        var visit = TestHelper.CreateVisit(isClaimed: true);

        var (sut, _, _) = TestHelper.CreateSupplierNotificationService();

        var result = await Should.ThrowAsync<InvalidOperationException>(() =>
            sut.NotifySupplierVisitRecordedAsync(booking, supplier, visit, TestHelper.SUPPORT_URL));

        result.Message.ShouldBe("Supplier.ContactEmail is not configured.");
    }
}
