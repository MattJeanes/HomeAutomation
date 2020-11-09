using HomeAutomation.Web.Data;
using System.Threading.Tasks;

namespace HomeAutomation.Web.Services.Interfaces
{
    public interface INotificationService
    {
        Task SendMessage(string message, MessagePriority messagePriority = MessagePriority.Normal);
    }
}
