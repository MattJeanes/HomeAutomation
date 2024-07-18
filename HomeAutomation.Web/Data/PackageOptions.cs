namespace HomeAutomation.Web.Data;

public class PackageOptions
{
    public class Location
    {
        public string Id { get; set; }
        public string ImageUrl { get; set; }
    }

    public string OpenAIApiKey { get; set; }
    public List<Location> Locations { get; set; }
}
