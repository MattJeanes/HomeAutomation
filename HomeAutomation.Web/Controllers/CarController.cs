using HomeAutomation.Web.Data;
using HomeAutomation.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace HomeAutomation.Web.Controllers
{
    [Route("api/[controller]")]
    public class CarController : BaseController
    {
        private readonly ITeslaService _teslaService;
        private readonly INotificationService _notificationService;

        public CarController(
            ITeslaService teslaService,
            INotificationService notificationService
            )
        {
            _teslaService = teslaService;
            _notificationService = notificationService;
        }

        [HttpPost]
        [Route("UnlockChargePort")]
        public async Task UnlockChargePort()
        {
            try
            {
                await _teslaService.UnlockChargePort();
            }
            catch (Exception e)
            {
                await _notificationService.SendMessage($"Failed to unlock car charge port:\n{e}");
                throw;
            }
        }

        [HttpPost]
        [Route("ToggleChargePort")]
        public async Task ToggleChargePort()
        {
            try
            {
                await _teslaService.ToggleChargePort();
            }
            catch (Exception e)
            {
                await _notificationService.SendMessage($"Failed to toggle car charge port:\n{e}");
                throw;
            }
        }

        [HttpPost]
        [Route("OpenBoot")]
        public async Task OpenBoot()
        {
            try
            {
                await _notificationService.SendMessage($"Opening car boot", MessagePriority.HighPriority);
                await _teslaService.OpenTrunk(false);
            }
            catch (Exception e)
            {
                await _notificationService.SendMessage($"Failed to open car boot:\n{e}");
                throw;
            }
        }

        [HttpPost]
        [Route("OpenHood")]
        public async Task OpenHood()
        {
            try
            {
                await _notificationService.SendMessage($"Opening car hood", MessagePriority.HighPriority);
                await _teslaService.OpenTrunk(true);
            }
            catch (Exception e)
            {
                await _notificationService.SendMessage($"Failed to open car hood:\n{e}");
                throw;
            }
        }

        [HttpPost]
        [Route("StartClimate")]
        public async Task StartClimate()
        {
            try
            {
                await _notificationService.SendMessage($"Turning on car climate", MessagePriority.HighPriority);
                await _teslaService.SetClimate(true);
            }
            catch (Exception e)
            {
                await _notificationService.SendMessage($"Failed to turn on car climate:\n{e}");
                throw;
            }
        }

        [HttpPost]
        [Route("StopClimate")]
        public async Task StopClimate()
        {
            try
            {
                await _notificationService.SendMessage($"Turning off car climate", MessagePriority.HighPriority);
                await _teslaService.SetClimate(false);
            }
            catch (Exception e)
            {
                await _notificationService.SendMessage($"Failed to turn off car climate:\n{e}");
                throw;
            }
        }
    }
}
