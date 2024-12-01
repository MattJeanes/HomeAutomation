using HomeAutomation.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeAutomation.Web.Controllers;

[Route("api/[controller]")]
public class PackageController : BaseController
{
    private readonly PackageOptions _options;
    private readonly HttpClient _httpClient;

    public PackageController(IOptions<PackageOptions> options, HttpClient httpClient)
    {
        _options = options.Value;
        _httpClient = httpClient;
    }

    [HttpGet]
    public async Task<PackageResponse> GetPackage()
    {
        var client = new ChatClient("gpt-4o-mini", _options.OpenAIApiKey);

        foreach (var location in _options.Locations)
        {
            var imageResponse = await _httpClient.GetAsync(location.ImageUrl);
            var imageBytes = await imageResponse.Content.ReadAsByteArrayAsync();

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(@"
Given a top-down CCTV image, analyze the image to determine if there is a package present.

A package is defined as any object that could be used for shipping or delivery, including but not limited to box-like objects, envelopes, and bag-shaped packages.

Ignore any other objects such as people, vehicles, doors, doormats, or any items that do not resemble a package.

Return a JSON object with a single property 'detected' that is a boolean value of `true' if a package is detected and `false` if no package is present. No other fields or output should be provided.

Steps to follow:

1. Examine the entire image for any shapes that could indicate the presence of a package, including rectangular or box-like shapes, flat and rectangular envelopes, and bag-like shapes.
2. Filter out any objects that do not fit the typical characteristics of packages (e.g., people, vehicles, irregularly shaped objects).
3. Make a determination based on the identified shapes and return true if any type of package (box, envelope, or bag) is found, otherwise return false.

Example Input:

An image showing a delivery area with various items, including a few packages and other objects.

Example Output:

If any type of package (box, envelope, or bag) is detected in the image: {""detected"": true}
If no package is detected in the image: {""detected"": false}
"),
                new UserChatMessage(ChatMessageContentPart.CreateImagePart(new BinaryData(imageBytes), "image/jpeg"))
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0,
                MaxOutputTokenCount = 10,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            var response = await client.CompleteChatAsync(messages, options);

            var responseText = response?.Value?.Content?.FirstOrDefault()?.Text;
            var chatResponse = JsonSerializer.Deserialize<ChatResponse>(responseText);
            if (chatResponse.Detected)
            {
                return new PackageResponse(location.Id, true);
            }
        }

        return new PackageResponse(null, false);
    }

    public class ChatResponse
    {
        [JsonPropertyName("detected")]
        public bool Detected { get; set; }
    }
}