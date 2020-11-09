using System.Threading.Tasks;

namespace HomeAutomation.Web.Services.Interfaces
{
    public interface ITeslaService
    {
        Task ManualChargePort();
        Task OpenTrunk(bool front);
        Task UnlockChargePort();
    }
}