using System.Diagnostics.CodeAnalysis;

namespace HomeAutomation.Web.Data;

public class ComputerOptions
{
    [NotNull]
    public string? MACAddress { get; set; }

    [NotNull]
    public string? BroadcastIP { get; set; }
}
