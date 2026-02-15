using Notifications.Worker.Domain;

namespace Notifications.UnitTests;

public sealed class NotificationLogTests
{
    [Fact]
    public void NotificationLog_ShouldDefaultToEmailChannel()
    {
        var log = new NotificationLog();

        log.Channel.Should().Be("email");
    }
}
