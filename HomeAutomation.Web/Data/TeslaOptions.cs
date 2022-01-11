using System.Diagnostics.CodeAnalysis;

namespace HomeAutomation.Web.Data;

public class TeslaOptions
{
    [NotNull]
    public string? OAuthClientUrl { get; set; }

    [NotNull]
    public string? AuthTokenUrl { get; set; }

    [NotNull]
    public string? RefreshToken { get; set; }

    [NotNull]
    public string? VIN { get; set; }
}
