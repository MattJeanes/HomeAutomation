namespace HomeAutomation.Web.Data;

public class PackageResponse
{
    public PackageResponse(bool package)
    {
        Package = package;
    }

    public bool Package { get; set; }
}
