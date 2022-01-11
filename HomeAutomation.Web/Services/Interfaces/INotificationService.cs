using HomeAutomation.Web.Data;

namespace HomeAutomation.Web.Services.Interfaces;

public interface INotificationService
{
    Task SendMessage(string message, MessagePriority messagePriority = MessagePriority.Normal);
}
