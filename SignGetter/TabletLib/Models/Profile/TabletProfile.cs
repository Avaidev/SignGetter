namespace TabletLib.Models.Profile;

public class TabletProfile
{
    public string DeviceName { get; set; } = "Unknown";
    public string Manufacturer { get; set; } = "Unknown";
    public int VendorId { get; set; }
    public int ProductId { get; set; }
    public string DataFormat { get; set; } = "Default";
    public DataStructure Data {  get; set; } = new DataStructure();
}