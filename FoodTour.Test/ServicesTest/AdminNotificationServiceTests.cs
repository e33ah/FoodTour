namespace FoodTour.Test.Services;

public class AdminNotificationServiceTests {
    [Fact]
    public async Task NotifyVisitRecordedAsync_DoesNotSendEmail_WhenNotificationsAreDisabled() {
        var booking = TestHelper.CreateValidBooking();
        var supplier = TestHelper.CreateSupplier(1);
        var visit = TestHelper.CreateVisit(isClaimed: true);
        var (sut, emailSenderMock, _) = TestHelper.CreateAdminNotificationService(notifyOnVisitRecorded: false);

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
        templateRendererMock.Setup(x => x.RenderTemplateAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync((string path, object model) => $"<html>Email body with {supplier.Name} and {TestHelper.formattedTourDate}</html>");

        await sut.NotifyVisitRecordedAsync(booking, supplier, visit);

        emailSenderMock.Verify(x => x.SendEmailAsync(
            TestHelper.ADMIN_EMAIL,
            It.Is<string>(subject => subject.Contains(supplier.Name)),
            It.Is<string>(body => body.Contains(TestHelper.formattedTourDate))
        ), Times.Once);
    }
}
