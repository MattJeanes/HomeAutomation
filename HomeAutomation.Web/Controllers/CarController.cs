using HomeAutomation.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace HomeAutomation.Web.Controllers
{
    [Route("api/[controller]")]
    public class CarController : BaseController
    {
        private readonly ITeslaService _teslaService;

        public CarController(ITeslaService teslaService)
        {
            _teslaService = teslaService;
        }

        [HttpPost]
        [Route("UnlockChargePort")]
        public async Task UnlockChargePort()
        {
            await _teslaService.UnlockChargePort();
        }

        [HttpPost]
        [Route("ManualChargePort")]
        public async Task ManualChargePort()
        {
            await _teslaService.ManualChargePort();
        }

        [HttpPost]
        [Route("OpenBoot")]
        public async Task OpenBoot()
        {
            await _teslaService.OpenTrunk(false);
        }

        [HttpPost]
        [Route("OpenHood")]
        public async Task OpenHood()
        {
            await _teslaService.OpenTrunk(true);
        }
    }
}
