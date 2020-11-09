using System.Threading.Tasks;

namespace HomeAutomation.Web.Services.Interfaces
{
    public interface IWakeOnLANService
    {
        Task WakeAsync(string macAddress, string broadcastIP);
    }
}
