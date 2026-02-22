namespace FoodTour.Test.ServicesTest;

public class AdminNotificationServiceTests {
    [Fact]
    public async Task NotifyVisitRecordedAsync_DoesNotSendEmail_WhenNotificationsAreDisabled() {
        var booking = TestHelper.CreateValidBooking();
        var supplier = TestHelper.CreateSupplier(1);
        var visit = TestHelper.CreateVisit();
        var emailSenderMock = new Mock<IEmailSender>();
        var options = Options.Create(new AdminNotificationSettings {
            AdminEmail = TestHelper.ADMIN_EMAIL,
            NotifyOnVisitRecorded = false
        });
        var sut = new AdminNotificationService(emailSenderMock.Object, options);

        await sut.NotifyVisitRecordedAsync(booking, supplier, visit);

        emailSenderMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task NotifyVisitRecordedAsync_Throws_WhenAdminEmailMissingAndNotificationsEnabled() {
        var booking = TestHelper.CreateValidBooking();
        var supplier = TestHelper.CreateSupplier(1);
        var visit = TestHelper.CreateVisit();
        var emailSenderMock = new Mock<IEmailSender>();
        var options = Options.Create(new AdminNotificationSettings {
            AdminEmail = "   ",
            NotifyOnVisitRecorded = true
        });
        var sut = new AdminNotificationService(emailSenderMock.Object, options);

        var result = await Should.ThrowAsync<InvalidOperationException>(() => sut.NotifyVisitRecordedAsync(booking, supplier, visit));

        result.Message.ShouldContain("AdminNotificationSettings.AdminEmail");
        emailSenderMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task NotifyVisitRecordedAsync_SendsEmail_WhenNotificationsAreEnabled() {
        var booking = TestHelper.CreateValidBooking();
        var supplier = TestHelper.CreateSupplier(1);
        var visit = TestHelper.CreateVisit();
        var claimedAt = new DateTime(2026, 6, 22, 15, 16, 39, DateTimeKind.Utc);
        visit.ClaimedAt = claimedAt;
        var emailSenderMock = new Mock<IEmailSender>();
        var options = Options.Create(new AdminNotificationSettings {
            AdminEmail = TestHelper.ADMIN_EMAIL,
            NotifyOnVisitRecorded = true
        });
        var sut = new AdminNotificationService(emailSenderMock.Object, options);


        await sut.NotifyVisitRecordedAsync(booking, supplier, visit);

        emailSenderMock.Verify(x => x.SendEmailAsync(
            TestHelper.ADMIN_EMAIL,
            It.Is<string>(subject => subject.Contains(supplier.Name)),
            It.Is<string>(body => body.Contains("15:16 22/06/2026"))
        ), Times.Once);
    }
}
