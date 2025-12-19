namespace TabletLib.Models;

public class DevicePreview
{
    public string Name { get; set; }
    public string Manufacturer  { get; set; }
    public int VendorID { get; private set; }
    public int ProductID { get; private set; }
    public Guid InstanceGuid { get; private set; }

    public DevicePreview(string name, string manufacturer, int vendorID, int productID, Guid instanceGuid)
    {
        Name = name;
        Manufacturer = manufacturer;
        VendorID = vendorID;
        ProductID = productID;
        InstanceGuid = instanceGuid;
    }
    
    public void ChangeVid(int vendorID){ VendorID = vendorID; }
    public void ChangePid(int productID){ ProductID = productID; }
    public void ChangeGuid(Guid instanceGuid) { InstanceGuid = instanceGuid; }

    public override string ToString()
    {
        return $"{Manufacturer} '{Name}' VID: {VendorID} PID: {ProductID}";
    }

    public override bool Equals(object? obj)
    {
        return InstanceGuid == (obj as DevicePreview)?.InstanceGuid;
    }
    
    public override int GetHashCode()
    {
        return InstanceGuid.GetHashCode();
    }
}