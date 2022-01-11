using HomeAutomation.Web.Data;
using HomeAutomation.Web.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace HomeAutomation.Web.Services;

public class PushoverService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly PushoverOptions _options;
    private readonly ILogger<PushoverService> _logger;

    public PushoverService(HttpClient httpClient, IOptions<PushoverOptions> options, ILogger<PushoverService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendMessage(string message, MessagePriority messagePriority = MessagePriority.Normal)
    {
        var response = await _httpClient.PostAsJsonAsync("1/messages.json", new PushRequest
        {
            Message = message,
            Priority = GetMessagePriority(messagePriority),
            Token = _options.ApiKey,
            User = _options.UserKey
        });
        var pushoverResponse = await response.Content.ReadFromJsonAsync<PushoverResponse>();
        if (pushoverResponse == null)
        {
            throw new Exception($"Failed to parse Pushover API response");
        }
        if (pushoverResponse.Status == PushoverResponseStatus.Invalid)
        {
            var err = $"Failed to call Pushover API: {string.Join(", ", pushoverResponse.Errors)}";
            _logger.LogError(err);
            throw new Exception(err);
        }
        else
        {
            response.EnsureSuccessStatusCode();
        }
    }

    private PushoverPriority GetMessagePriority(MessagePriority messagePriority)
    {
        return messagePriority switch
        {
            MessagePriority.Silent => PushoverPriority.AlwaysQuiet,
            MessagePriority.Normal => PushoverPriority.Normal,
            MessagePriority.HighPriority => PushoverPriority.HighPriority,
            MessagePriority.Critical => PushoverPriority.UserConfirmation,
            _ => throw new InvalidEnumArgumentException(nameof(messagePriority)),
        };
    }

    private struct PushRequest
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("user")]
        public string User { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("priority")]
        public PushoverPriority Priority { get; set; }
    }

    private enum PushoverPriority
    {
        NoNotificationAlert = -2,
        AlwaysQuiet = -1,
        Normal = 0,
        HighPriority = 1,
        UserConfirmation = 2
    }

    private enum PushoverResponseStatus
    {
        Valid = 1,
        Invalid = 0
    }

    private class PushoverResponse
    {
        [JsonPropertyName("errors")]
        [NotNull]
        public List<string>? Errors { get; set; }

        [JsonPropertyName("status")]
        public PushoverResponseStatus Status { get; set; }
    }
}
