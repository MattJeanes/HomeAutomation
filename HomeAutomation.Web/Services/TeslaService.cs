using HomeAutomation.Web.Data;
using HomeAutomation.Web.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace HomeAutomation.Web.Services;

public class TeslaService : ITeslaService
{
    private readonly HttpClient _client;
    private readonly TeslaOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TeslaService> _logger;
    private readonly Regex ClientUrlRegex = new Regex("^.+=(.+?)\r?$", RegexOptions.Multiline);
    private const string TokenCacheKey = "Token";
    private const string CarIdCacheKey = "CarId";
    private const string WakeUpUrl = "/wake_up";

    public TeslaService(
        HttpClient client,
        IOptions<TeslaOptions> options,
        IMemoryCache cache,
        ILogger<TeslaService> logger
        )
    {
        _client = client;
        _options = options.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task UnlockChargePort()
    {
        _logger.LogInformation("Unlocking charge port");
        var chargeState = await RunCommand<ChargeState>("/data_request/charge_state", HttpMethod.Get);
        if (!chargeState.ChargePortDoorOpen)
        {
            _logger.LogInformation("Charge port is not open");
        }
        else if (chargeState.ChargePortLatch != "Engaged")
        {
            _logger.LogInformation("Charge port latch is already disengaged");
        }
        else if (chargeState.ChargingState == "Charging")
        {
            _logger.LogInformation("Car is currently charging");
        }
        else
        {
            _logger.LogInformation("Charge port is opened and engaged, unlocking");
            var openCommand = await RunCommand<Command>("/command/charge_port_door_open", HttpMethod.Post);
            if (!openCommand.Result)
            {
                throw new Exception($"Charge port failed to unlock: {openCommand.Reason}");
            }
            else
            {
                _logger.LogInformation("Charge port unlocked");
            }
        }
    }

    public async Task ToggleChargePort()
    {
        _logger.LogInformation("Toggle charge port");
        var chargeState = await RunCommand<ChargeState>("/data_request/charge_state", HttpMethod.Get);
        var open = true;
        if (chargeState.ChargePortDoorOpen && chargeState.ChargePortLatch != "Engaged")
        {
            _logger.LogInformation("Charge port door open but unlatched, closing");
            open = false;
        }
        else if (chargeState.ChargingState == "Charging")
        {
            _logger.LogInformation("Car currently charging, stopping");
            var chargeStopCommand = await RunCommand<Command>("/command/charge_stop", HttpMethod.Post);
            if (!chargeStopCommand.Result)
            {
                throw new Exception($"Charge stop failed: {chargeStopCommand.Reason}");
            }
            else
            {
                _logger.LogInformation("Car charging stopped");
            }
        }

        var chargePortDoorCommand = await RunCommand<Command>($"/command/charge_port_door_{(open ? "open" : "close")}", HttpMethod.Post);
        if (!chargePortDoorCommand.Result)
        {
            throw new Exception($"Charge port command failed: ${chargePortDoorCommand.Reason}");
        }
        else
        {
            _logger.LogInformation($"Charge port door {(open ? "opened" : "closed")}");
        }
    }

    public async Task OpenTrunk(bool front)
    {
        _logger.LogInformation($"Opening {(front ? "hood" : "boot")}");
        var openTrunkCommand = await RunCommand<Command>("/command/actuate_trunk", HttpMethod.Post, new { which_trunk = front ? "front" : "rear" });
        if (!openTrunkCommand.Result)
        {
            throw new Exception($"Open trunk command failed: ${openTrunkCommand.Reason}");
        }
        else
        {
            _logger.LogInformation($"{(front ? "Hood" : "Boot")} opened");
        }
    }

    public async Task SetClimate(bool enable)
    {
        _logger.LogInformation($"{(enable ? "Starting" : "Stopping")} climate");
        var climateCommand = await RunCommand<Command>($"/command/auto_conditioning_{(enable ? "start" : "stop")}", HttpMethod.Post);
        if (!climateCommand.Result)
        {
            throw new Exception($"Climate control command failed: ${climateCommand.Reason}");
        }
        else
        {
            _logger.LogInformation($"Climate {(enable ? "started" : "stopped")}");
        }
    }

    public async Task SetTemperature(float temperature)
    {
        _logger.LogInformation($"Setting climate temperature to {temperature} degrees");
        var climateState = await RunCommand<ClimateState>("/data_request/climate_state", HttpMethod.Get);
        if (!climateState.IsClimateOn)
        {
            _logger.LogInformation("Climate is not on, starting");
            var climateStartCommand = await RunCommand<Command>("/command/auto_conditioning_start", HttpMethod.Post);
            if (!climateStartCommand.Result)
            {
                throw new Exception($"Climate start command failed: ${climateStartCommand.Reason}");
            }
            else
            {
                _logger.LogInformation($"Climate started");
            }
        }
        var climateTemperatureCommand = await RunCommand<Command>("/command/set_temps", HttpMethod.Post, new { driver_temp = temperature, passenger_temp = temperature });
        if (!climateTemperatureCommand.Result)
        {
            throw new Exception($"Climate temperature command failed: ${climateTemperatureCommand.Reason}");
        }
        else
        {
            _logger.LogInformation($"Climate temperature set");
        }
    }

    private async Task<T> RunCommand<T>(string url, HttpMethod method, object? data = null) where T : class
    {
        if (!_cache.TryGetValue<string>(CarIdCacheKey, out var carId) && !string.IsNullOrEmpty(url))
        {
            var vehicles = await RunCommand<List<Vehicle>>(string.Empty, HttpMethod.Get);
            var vehicle = vehicles.FirstOrDefault(x => x.VIN == _options.VIN);
            if (vehicle == null)
            {
                throw new Exception($"Could not find car with VIN {_options.VIN}");
            }
            carId = vehicle.Id;
            _cache.Set(CarIdCacheKey, carId);
        }

        var message = new HttpRequestMessage()
        {
            Method = method,
            RequestUri = new Uri(_client.BaseAddress!, $"/api/1/vehicles/{carId}{url}")
        };
        if (data != null)
        {
            message.Content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
        }
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetBearerToken());
        var response = await _client.SendAsync(message);

        if (url != WakeUpUrl)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.RequestTimeout) // Car is asleep
            {
                _logger.LogInformation("Car is asleep, waking");
                var wakeTries = 0;
                while (true)
                {
                    var wakeUpResponse = await RunCommand<WakeUp>(WakeUpUrl, HttpMethod.Post);
                    if (wakeUpResponse.State == "online")
                    {
                        break;
                    }
                    wakeTries++;
                    if (wakeTries == 10)
                    {
                        throw new Exception("Failed to wake car");
                    }
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
                _logger.LogInformation("Car is awake, running original command");
                return await RunCommand<T>(url, method, data);
            }
            response.EnsureSuccessStatusCode();
        }

        var teslaResponse = await response.Content.ReadFromJsonAsync<TeslaResponse<T>>();
        if (teslaResponse == null)
        {
            throw new Exception("Failed to parse Tesla API repsonse");
        }
        return teslaResponse.Response;
    }

    private async Task<string> GetBearerToken()
    {
        if (!_cache.TryGetValue<TokenResponse>(TokenCacheKey, out var tokenResponse))
        {
            var response = await _client.GetAsync(_options.OAuthClientUrl);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var matches = ClientUrlRegex.Matches(content);
            var clientId = matches[0].Groups[1].Value;
            var clientSecret = matches[1].Groups[1].Value;

            var requestContent = new StringContent(JsonSerializer.Serialize(new
            {
                grant_type = "refresh_token",
                client_id = "ownerapi",
                refresh_token = _options.RefreshToken,
                scope = "openid email offline_access"
            }), Encoding.UTF8, "application/json");
            requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            response = await _client.PostAsync(_options.AuthTokenUrl, requestContent);
            response.EnsureSuccessStatusCode();
            tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
            if (tokenResponse == null)
            {
                throw new Exception("Failed to parse Tesla API repsonse");
            }

            _cache.Set(TokenCacheKey, tokenResponse, TimeSpan.FromSeconds(tokenResponse.ExpiresIn - 30));
        }

        return tokenResponse.AccessToken;
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        [NotNull]
        public string? AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        [NotNull]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        [NotNull]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }
    }

    private class TeslaResponse<T>
    {
        [JsonPropertyName("response")]
        [NotNull]
        public T? Response { get; set; }
    }

    private class WakeUp
    {
        [JsonPropertyName("state")]
        [NotNull]
        public string? State { get; set; }
    }

    private class Vehicle
    {
        [JsonPropertyName("id_s")]
        [NotNull]
        public string? Id { get; set; }

        [JsonPropertyName("vin")]
        [NotNull]
        public string? VIN { get; set; }
    }

    private class ChargeState
    {
        [JsonPropertyName("charge_port_door_open")]
        public bool ChargePortDoorOpen { get; set; }

        [JsonPropertyName("charge_port_latch")]
        [NotNull]
        public string? ChargePortLatch { get; set; }

        [JsonPropertyName("charging_state")]
        [NotNull]
        public string? ChargingState { get; set; }
    }

    private class Command
    {
        [JsonPropertyName("result")]
        public bool Result { get; set; }

        [JsonPropertyName("reason")]
        [NotNull]
        public string? Reason { get; set; }
    }

    private class ClimateState
    {
        [JsonPropertyName("is_climate_on")]
        public bool IsClimateOn { get; set; }
    }
}
