namespace FoodTour.Test.Services;

public class AdminNotificationServiceTests {
    [Fact]
    public async Task NotifyVisitRecordedAsync_DoesNotSendEmail_WhenNotificationsAreDisabled() {
        var booking = TestHelper.CreateValidBooking();
        var supplier = TestHelper.CreateSupplier(1);
        var visit = TestHelper.CreateVisit(isClaimed: true);
        var (sut, emailSenderMock, _) = TestHelper.CreateAdminNotificationService(notifyOnVisitRecorded: false);

        await sut.NotifyVisitRecordedAsync(booking, supplier, visit);

        emailSenderMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task NotifyVisitRecordedAsync_Throws_WhenAdminEmailMissingAndNotificationsEnabled() {
        var booking = TestHelper.CreateValidBooking();
        var supplier = TestHelper.CreateSupplier(1);
        var visit = TestHelper.CreateVisit(isClaimed: true);
        var (sut, emailSenderMock, _) = TestHelper.CreateAdminNotificationService(adminEmail: "   ");

        var result = await Should.ThrowAsync<InvalidOperationException>(() => sut.NotifyVisitRecordedAsync(booking, supplier, visit));

        result.Message.ShouldContain("AdminNotificationSettings.AdminEmail");
        emailSenderMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task NotifyVisitRecordedAsync_SendsEmail_WhenNotificationsAreEnabled() {
        var booking = TestHelper.CreateValidBooking();
        var supplier = TestHelper.CreateSupplier(1);
        var visit = TestHelper.CreateVisit(isClaimed: true);
        var (sut, emailSenderMock, templateRendererMock) = TestHelper.CreateAdminNotificationService();
        string expectedBody = $"<html>Email body with {supplier.Name} and {TestHelper.formattedTourDate}</html>";
        templateRendererMock.Setup(x => x.RenderTemplateAsync("Emails/AdminVisitRecorded", It.IsAny<object>()))
            .ReturnsAsync(expectedBody);

        await sut.NotifyVisitRecordedAsync(booking, supplier, visit);

        templateRendererMock.Verify(x => x.RenderTemplateAsync("Emails/AdminVisitRecorded", It.IsAny<object>()), Times.Once);
        emailSenderMock.Verify(x => x.SendEmailAsync(
            TestHelper.ADMIN_EMAIL,
            It.Is<string>(subject => subject.Contains(supplier.Name)),
            expectedBody
        ), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NotifyVisitRecordedAsync_Throws_WhenSupplierNameIsBlank(string supplierName) {
        var booking = TestHelper.CreateValidBooking();
        var supplier = TestHelper.CreateSupplier(1);
        supplier.Name = supplierName;
        var visit = TestHelper.CreateVisit(isClaimed: true);
        var (sut, emailSenderMock, _) = TestHelper.CreateAdminNotificationService();

        var result = await Should.ThrowAsync<InvalidOperationException>(() => sut.NotifyVisitRecordedAsync(booking, supplier, visit));

        result.Message.ShouldBe("Supplier.Name is required.");
        emailSenderMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NotifyVisitRecordedAsync_Throws_WhenCustomerEmailIsBlank(string customerEmail) {
        var booking = TestHelper.CreateValidBooking();
        booking.CustomerEmail = customerEmail;
        var supplier = TestHelper.CreateSupplier(1);
        var visit = TestHelper.CreateVisit(isClaimed: true);
        var (sut, emailSenderMock, _) = TestHelper.CreateAdminNotificationService();

        var result = await Should.ThrowAsync<InvalidOperationException>(() => sut.NotifyVisitRecordedAsync(booking, supplier, visit));

        result.Message.ShouldBe("Booking.CustomerEmail is required.");
        emailSenderMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task NotifyVisitRecordedAsync_Throws_WhenClaimedAtIsMissing() {
        var booking = TestHelper.CreateValidBooking();
        var supplier = TestHelper.CreateSupplier(1);
        var visit = TestHelper.CreateVisit(isClaimed: false);
        var (sut, emailSenderMock, _) = TestHelper.CreateAdminNotificationService();

        var result = await Should.ThrowAsync<InvalidOperationException>(() => sut.NotifyVisitRecordedAsync(booking, supplier, visit));

        result.Message.ShouldBe("Visit.ClaimedAt is required.");
        emailSenderMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
