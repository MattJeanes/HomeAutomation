using FFMpegCore;
using HomeAutomation.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeAutomation.Web.Controllers;

[Route("api/[controller]")]
public class BoilerController : Controller
{
    private readonly BoilerOptions _options;

    public BoilerController(IOptions<BoilerOptions> options)
    {
        _options = options.Value;
    }

    [HttpGet]
    public async Task<BoilerResponse> GetBoilerPressure()
    {
        var framePath = await GetFrame();
        // System.IO.File.Copy(framePath, @"E:\Matt\Downloads\testing\frame.jpg", true);
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "python3",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            processStartInfo.ArgumentList.Add("./BoilerGaugeReader/analog_gauge_reader.py");
            processStartInfo.ArgumentList.Add($"--gauge_radius={_options.GaugeRadius}");
            processStartInfo.ArgumentList.Add($"--min_angle={_options.MinAngle}");
            processStartInfo.ArgumentList.Add($"--max_angle={_options.MaxAngle}");
            processStartInfo.ArgumentList.Add($"--min_value={_options.MinValue}");
            processStartInfo.ArgumentList.Add($"--max_value={_options.MaxValue}");
            processStartInfo.ArgumentList.Add($"--min_needle_size={_options.MinNeedleSize}");
            processStartInfo.ArgumentList.Add(framePath);
            var process = new Process();
            process.StartInfo = processStartInfo;
            BoilerInternalResponse internalResponse = null;
            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    internalResponse = JsonSerializer.Deserialize<BoilerInternalResponse>(args.Data);
                }
            };
            process.Start();
            process.BeginOutputReadLine();
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                throw new Exception($"Python exited with code {process.ExitCode}");
            }

            if (internalResponse == null)
            {
                throw new Exception("Failed to get response from Python");
            }

            var pressureBar = Math.Clamp((float)Math.Round(internalResponse.Value, 1), _options.MinValue, _options.MaxValue);

            return new BoilerResponse
            {
                PressureBar = pressureBar
            };
        }
        finally
        {
            System.IO.File.Delete(framePath);
        }
    }

    private async Task<string> GetFrame()
    {
        var tempFileName = Path.GetTempPath() + Guid.NewGuid().ToString() + ".jpg";

        await FFMpegArguments
            .FromUrlInput(_options.RtspUrl)
            .OutputToFile(tempFileName, addArguments: options => options
                .WithFrameOutputCount(1))
            .ProcessAsynchronously();

        return tempFileName;
    }

    public class BoilerInternalResponse
    {
        [JsonPropertyName("angle")]
        public float Angle { get; set; }

        [JsonPropertyName("value")]
        public float Value { get; set; }
    }

    public class BoilerResponse
    {
        public float PressureBar { get; set; }
    }
}
