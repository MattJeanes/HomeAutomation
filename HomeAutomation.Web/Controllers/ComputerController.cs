using HomeAutomation.Web.Data;
using HomeAutomation.Web.Helpers.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace HomeAutomation.Web.Controllers
{
    [Route("api/[controller]")]
    public class ComputerController : BaseController
    {
        private readonly IWakeOnLANHelper _wolHelper;
        private readonly ILogger<ComputerController> _logger;
        private readonly ComputerOptions _options;

        public ComputerController(IWakeOnLANHelper wolHelper, ILogger<ComputerController> logger, IOptions<ComputerOptions> options)
        {
            _wolHelper = wolHelper;
            _logger = logger;
            _options = options.Value;
        }

        [HttpPost]
        [Route("on")]
        public async Task On()
        {
            _logger.LogInformation($"Turning on computer {_options.MACAddress} using broadcast IP {_options.BroadcastIP}");
            await _wolHelper.WakeAsync(_options.MACAddress, _options.BroadcastIP);
        }
    }
}
