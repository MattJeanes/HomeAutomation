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
        public ActionResult UnlockChargePort()
        {
            Response.OnCompleted(async () =>
            {
                try
                {
                    await _teslaService.UnlockChargePort();
                }
                catch (Exception e)
                {
                    await _notificationService.SendMessage($"Failed to unlock car charge port:\n{e}", MessagePriority.HighPriority);
                }
            });
            return Accepted();
        }

        [HttpPost]
        [Route("ToggleChargePort")]
        public ActionResult ToggleChargePort()
        {
            Response.OnCompleted(async () =>
            {
                try
                {
                    await _teslaService.ToggleChargePort();
                }
                catch (Exception e)
                {
                    await _notificationService.SendMessage($"Failed to toggle car charge port:\n{e}", MessagePriority.HighPriority);
                }
            });
            return Accepted();
        }

        [HttpPost]
        [Route("OpenBoot")]
        public ActionResult OpenBoot()
        {
            Response.OnCompleted(async () =>
            {
                try
                {
                    await _notificationService.SendMessage($"Opening car boot", MessagePriority.HighPriority);
                    await _teslaService.OpenTrunk(false);
                }
                catch (Exception e)
                {
                    await _notificationService.SendMessage($"Failed to open car boot:\n{e}", MessagePriority.HighPriority);
                }
            });
            return Accepted();
        }

        [HttpPost]
        [Route("OpenHood")]
        public ActionResult OpenHood()
        {
            Response.OnCompleted(async () =>
            {
                try
                {
                    await _notificationService.SendMessage($"Opening car hood", MessagePriority.HighPriority);
                    await _teslaService.OpenTrunk(true);
                }
                catch (Exception e)
                {
                    await _notificationService.SendMessage($"Failed to open car hood:\n{e}", MessagePriority.HighPriority);
                }
            });
            return Accepted();
        }

        [HttpPost]
        [Route("StartClimate")]
        public ActionResult StartClimate()
        {
            Response.OnCompleted(async () =>
            {
                try
                {
                    await _notificationService.SendMessage($"Turning on car climate", MessagePriority.HighPriority);
                    await _teslaService.SetClimate(true);
                }
                catch (Exception e)
                {
                    await _notificationService.SendMessage($"Failed to turn on car climate:\n{e}", MessagePriority.HighPriority);
                }
            });
            return Accepted();
        }

        [HttpPost]
        [Route("StopClimate")]
        public ActionResult StopClimate()
        {
            Response.OnCompleted(async () =>
            {
                try
                {
                    await _notificationService.SendMessage($"Turning off car climate", MessagePriority.HighPriority);
                    await _teslaService.SetClimate(false);
                }
                catch (Exception e)
                {
                    await _notificationService.SendMessage($"Failed to turn off car climate:\n{e}", MessagePriority.HighPriority);
                }
            });
            return Accepted();
        }

        [HttpPost]
        [Route("SetTemperature/{temperature:int}")]
        public ActionResult SetTemperature([FromRoute] float temperature)
        {
            Response.OnCompleted(async () =>
            {
                try
                {
                    await _notificationService.SendMessage($"Setting car temperature to {temperature} degrees", MessagePriority.HighPriority);
                    await _teslaService.SetTemperature(temperature);
                }
                catch (Exception e)
                {
                    await _notificationService.SendMessage($"Failed to turn off car climate:\n{e}", MessagePriority.HighPriority);
                }
            });
            return Accepted();
        }
    }
}
