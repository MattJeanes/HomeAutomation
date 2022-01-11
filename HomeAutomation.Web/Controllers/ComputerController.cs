using HomeAutomation.Web.Data;
using HomeAutomation.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HomeAutomation.Web.Controllers;

[Route("api/[controller]")]
public class ComputerController : BaseController
{
    private readonly IWakeOnLANService _wolHelper;
    private readonly ILogger<ComputerController> _logger;
    private readonly ComputerOptions _options;
    private readonly INotificationService _notificationService;

    public ComputerController(
        IWakeOnLANService wolHelper,
        ILogger<ComputerController> logger,
        IOptions<ComputerOptions> options,
        INotificationService notificationService
        )
    {
        _wolHelper = wolHelper;
        _logger = logger;
        _options = options.Value;
        _notificationService = notificationService;
    }

    [HttpPost]
    [Route("on")]
    public async Task On()
    {
        try
        {
            _logger.LogInformation($"Turning on computer {_options.MACAddress} using broadcast IP {_options.BroadcastIP}");
            await _wolHelper.WakeAsync(_options.MACAddress, _options.BroadcastIP);
        }
        catch (Exception e)
        {
            await _notificationService.SendMessage($"Failed to turn on computer:\n{e}");
            throw;
        }
    }
}
