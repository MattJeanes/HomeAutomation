using System.Threading.Tasks;

namespace HomeAutomation.Web.Helpers.Interfaces
{
    public interface IWakeOnLANHelper
    {
        Task WakeAsync(string macAddress, string broadcastIP);
    }
}
