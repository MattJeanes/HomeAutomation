using DotnetGeminiSDK.Client.Interfaces;
using DotnetGeminiSDK.Model.Request;
using FFMpegCore;
using FFMpegCore.Pipes;
using HomeAutomation.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Reflection;
using System.Text.Json;

namespace HomeAutomation.Web.Controllers;

[Route("api/[controller]")]
public class BoilerController : Controller
{
    private readonly BoilerOptions _options;
    private readonly IGeminiClient _geminiClient;

    public BoilerController(IOptions<BoilerOptions> options, IGeminiClient geminiClient)
    {
        _options = options.Value;
        _geminiClient = geminiClient;
    }

    [HttpGet]
    public async Task<BoilerResponse> GetBoilerPressure()
    {
        var frame = await GetFrame();
        var reference = await GetReference();

        await System.IO.File.WriteAllBytesAsync(@"E:\Matt\Downloads\testing\frame.jpg", frame);
        await System.IO.File.WriteAllBytesAsync(@"E:\Matt\Downloads\testing\reference.jpg", reference);

        return await GetResponse(frame, reference);
    }

    private async Task<byte[]> GetReference()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "HomeAutomation.Web.Assets.BoilerReference.jpg";

        using var stream = new MemoryStream();
        using var resourceStream = assembly.GetManifestResourceStream(resourceName);
        await resourceStream.CopyToAsync(stream);
        return stream.ToArray();
    }

    private async Task<byte[]> GetFrame()
    {
        using var stream = new MemoryStream();

        await FFMpegArguments
            .FromUrlInput(_options.RtspUrl)
            .OutputToPipe(new StreamPipeSink(stream), options => options
                .WithFrameOutputCount(1)
                .ForceFormat("image2pipe"))
            .ProcessAsynchronously();

        return stream.ToArray();
    }

    private async Task<BoilerResponse> GetResponse(byte[] frame, byte[] reference)
    {
        var content = new List<Content>
        {
            new Content
            {
                Parts  = new List<Part>
                {
                    new Part
                    {
                        InlineData = new InlineData
                        {
                            MimeType = "image/jpeg",
                            Data = Convert.ToBase64String(frame)
                        }
                    },
                    new Part
                    {
                        InlineData = new InlineData
                        {
                            MimeType = "image/jpeg",
                            Data = Convert.ToBase64String(reference)
                        }
                    },
                    new Part
                    {
                        Text = @"
The first picture is a view from a camera looking up at a boiler.

The second picture is a reference picture of the pressure meter.

Locate the pressure meter in the first picture using the reference picture from the second.

Look at the meter hand within the pressure meter to approximate the current boiler pressure.

Make sure to take account for the camera angle that will warp how the dial looks from below.
        "
                    }
                }
            }
        };
        var generationConfig = new GeminiGenerationConfig
        {
            Temperature = 1,
            TopK = 40,
            TopP = 0.95,
            MaxOutputTokens = 128,
            ResponseMimeType = "application/json",
            ResponseSchema = new
            {
                type = "object",
                properties = new
                {
                    pressureBar = new
                    {
                        type = "number"
                    }
                }
            }
        };
        var resp = await _geminiClient.TextPrompt(content, generationConfig);
        var text = resp.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

        if (text == null)
        {
            throw new Exception("Unexpected error while generating response");
        }

        return JsonConvert.DeserializeObject<BoilerResponse>(text);
    }

    public class GeminiGenerationConfig : GenerationConfig
    {
        [JsonProperty("response_mime_type")]
        public string ResponseMimeType { get; set; } = "application/json";

        [JsonProperty("response_schema")]
        public object ResponseSchema { get; set; }
    }

    public class BoilerResponse
    {
        [JsonProperty("pressureBar")]
        public float PressureBar { get; set; }
    }
}
