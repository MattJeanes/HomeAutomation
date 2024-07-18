namespace HomeAutomation.Web.Data;

public class PackageResponse
{
    public PackageResponse(string locationId, bool detected)
    {
        LocationId = locationId;
        Detected = detected;
    }

    public string LocationId { get; set; }
    public bool Detected { get; set; }
}
