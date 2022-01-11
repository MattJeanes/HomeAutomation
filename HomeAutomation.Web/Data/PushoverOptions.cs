using System.Diagnostics.CodeAnalysis;

namespace HomeAutomation.Web.Data;

public class PushoverOptions
{
    [NotNull]
    public string? ApiKey { get; set; }

    [NotNull]
    public string? UserKey { get; set; }
}
