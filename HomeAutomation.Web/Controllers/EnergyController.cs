﻿using Microsoft.AspNetCore.Mvc;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System.Net;
using System.Text.Json.Nodes;

namespace HomeAutomation.Web.Controllers;

[Route("api/[controller]")]
public class EnergyController : BaseController
{
    private readonly MqttFactory _mqttFactory;
    private readonly MqttClientOptions _mqttClientOptions;
    private readonly ILogger<EnergyController> _logger;
    private static string _correlationIdProperty = "correlationId";

    public EnergyController(MqttFactory mqttFactory, MqttClientOptions mqttClientOptions, ILogger<EnergyController> logger)
    {
        _mqttFactory = mqttFactory;
        _mqttClientOptions = mqttClientOptions;
        _logger = logger;
    }

    [HttpPost("{**slug}")]
    public async Task<IActionResult> Invoke(string slug, [FromBody] JsonObject request)
    {
        using var mqttClient = _mqttFactory.CreateMqttClient();
        await mqttClient.ConnectAsync(_mqttClientOptions);
        await mqttClient.SubscribeAsync("energy/solar/result");

        var correlationId = Guid.NewGuid().ToString();
        request[_correlationIdProperty] = correlationId;

        JsonObject response = null;
        mqttClient.ApplicationMessageReceivedAsync += (message) =>
        {
            JsonObject tempResponse;
            try
            {
                tempResponse = JsonNode.Parse(message.ApplicationMessage.PayloadSegment).AsObject();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse MQTT response");
                return Task.CompletedTask;
            }
            var receivedCorrelationId = tempResponse[_correlationIdProperty].ToString();
            if (receivedCorrelationId == correlationId)
            {
                tempResponse.Remove(_correlationIdProperty);
                response = tempResponse;
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

        return Ok(response);
    }
}