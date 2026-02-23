namespace FoodTour.Test.ServicesTest;

public class SupplierNotificationServiceTests {
    [Fact]
    public async Task NotifyVisitRecordedAsync_DoesNotSendEmail_WhenEmailNotificationsAreDisabled() {
        var booking = TestHelper.CreateValidBooking();
        var supplier = TestHelper.CreateSupplier(1);
        supplier.EmailNotificationsEnabled = false;
        var visit = TestHelper.CreateVisit();
        var emailSenderMock = new Mock<IEmailSender>();
        var sut = new SupplierNotificationService(emailSenderMock.Object);

        await sut.NotifyVisitRecordedAsync(booking, supplier, visit, TestHelper.SUPPORT_URL);

        emailSenderMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task NotifyVisitRecordedAsync_SendsEmail_WhenEmailNotificationsAreEnabled() {
        var booking = TestHelper.CreateValidBooking();
        var supplier = TestHelper.CreateSupplier(1);
        supplier.EmailNotificationsEnabled = true;
        var visit = TestHelper.CreateVisit();
        var emailSenderMock = new Mock<IEmailSender>();
        var sut = new SupplierNotificationService(emailSenderMock.Object);

        await sut.NotifyVisitRecordedAsync(booking, supplier, visit, TestHelper.SUPPORT_URL);

        emailSenderMock.Verify(x => x.SendEmailAsync(
            supplier.ContactEmail,
            It.Is<string>(subject => subject.Contains(TestHelper.formattedTourDate)),
            It.Is<string>(body => body.Contains(supplier.Name))
        ), Times.Once);
    }
}
