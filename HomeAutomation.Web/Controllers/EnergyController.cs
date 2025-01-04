using HomeAutomation.Web.Data;
using Microsoft.AspNetCore.Mvc;
using MQTTnet;
using MQTTnet.Protocol;
using System.Net;
using System.Text.Json.Nodes;

namespace HomeAutomation.Web.Controllers;

[Route("api/[controller]")]
public class EnergyController : BaseController
{
    private readonly MqttClientFactory _mqttClientFactory;
    private readonly MqttClientOptions _mqttClientOptions;
    private readonly ILogger<EnergyController> _logger;

    public EnergyController(MqttClientFactory mqttClientFactory, MqttClientOptions mqttClientOptions, ILogger<EnergyController> logger)
    {
        _mqttClientFactory = mqttClientFactory;
        _mqttClientOptions = mqttClientOptions;
        _logger = logger;
    }

    [HttpPost("{**slug}")]
    public async Task<IActionResult> Invoke(string slug, [FromBody] JsonObject request)
    {
        if (slug == "modbus/set")
        {
            return BadRequest(new InverterResponse
            {
                Success = false,
                Message = "Raw modbus set commands are blocked over the HTTP Proxy"
            });
        }

        using var mqttClient = _mqttClientFactory.CreateMqttClient();
        await mqttClient.ConnectAsync(_mqttClientOptions);
        await mqttClient.SubscribeAsync("energy/solar/result");

        var correlationId = Guid.NewGuid().ToString();
        request["correlationId"] = correlationId;

        JsonObject response = null;
        mqttClient.ApplicationMessageReceivedAsync += (message) =>
        {
            JsonObject tempResponse;
            try
            {
                tempResponse = JsonNode.Parse(message.ApplicationMessage.ConvertPayloadToString()).AsObject();
                if (tempResponse.TryGetPropertyValue("correlationId", out var correlationIdNode) && correlationIdNode.GetValue<string>() == correlationId)
                {
                    response = tempResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse MQTT response");
                return Task.CompletedTask;
            }
            return Task.CompletedTask;
        };

        var message = new MqttApplicationMessageBuilder()
            .WithTopic($"energy/solar/command/{slug}")
            .WithPayload(request.ToJsonString())
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
            .Build();

        await mqttClient.PublishAsync(message);

        var timeout = DateTime.UtcNow.AddSeconds(10);

        while (response == null)
        {
            await Task.Delay(100);

            if (DateTime.UtcNow > timeout)
            {
                return StatusCode((int)HttpStatusCode.BadGateway);
            }
        }

        var success = response["success"].GetValue<bool>();

        if (success)
        {
            return Ok(response);
        }
        else
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, response);
        }
    }
}
