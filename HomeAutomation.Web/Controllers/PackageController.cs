﻿using HomeAutomation.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenAI_API;
using OpenAI_API.Chat;

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
        var openAI = new OpenAIAPI(new APIAuthentication(_options.OpenAIApiKey));

        foreach (var location in _options.Locations)
        {
            var imageResponse = await _httpClient.GetAsync(location.ImageUrl);
            var imageBytes = await imageResponse.Content.ReadAsByteArrayAsync();

            var chatRequest = new ChatRequest
            {
                Model = "gpt-4o-mini",
                Temperature = 0,
                MaxTokens = 10,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                Messages =
                [
                    new ChatMessage
                    {
                        Role = ChatMessageRole.System,
                        TextContent = @"
You are an AI assistant designed to help the user determine if there is a delivery left on their doorstep and notify them if there is.

They will upload a photo of a top down view from a CCTV camera which may be in color or black and white. It may be taken during the day or at night.

The image may contain a delivery, or it may not. Your job is to determine if there is a delivery in the image.

The delivery may take any shape or form, such as an envelope, a box or a bag. It may be any size and may be placed and oriented in any way.

Ignore any other objects in the image, such as the doorstep, walls, doormat, people, cars, trees, or any other objects that are not part of the delivery.

Your job is to return only the word 'true' or 'false' depending on if you see a delivery or not.

Do not say anything except 'true' or 'false', as the output will be processed by code.
",

                    },
                    new() {
                        Role = ChatMessageRole.User,
                        Images =
                        [
                            new ChatMessage.ImageInput(imageBytes)
                        ]
                    },
                ]
            };

            var response = await openAI.Chat.CreateChatCompletionAsync(chatRequest);

            var responseText = response?.Choices?.FirstOrDefault()?.Message.TextContent;
            if (bool.Parse(responseText))
            {
                return new PackageResponse(location.Id, true);
            }
        }

        return new PackageResponse(null, false);
    }
}
