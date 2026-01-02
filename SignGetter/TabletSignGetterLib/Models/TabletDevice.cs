using HidSharp;

namespace TabletSignGetterLib.Models;

public class TabletDevice
{
    public string DeviceName { get; private set; }
    public string Manufacturer { get; private set; }
    public readonly int VendorId;
    public readonly int ProductId;

    private int _logicalMaxX;
    private int _logicalMaxY;

    public TabletDevice(HidDevice device)
    {
        DeviceName = device.GetProductName();
        Manufacturer = device.GetManufacturer();
        VendorId = device.VendorID;
        ProductId = device.ProductID;
        DetermineMax(device.GetRawReportDescriptor());
    }
    
    private void DetermineMax(byte[] descriptor)
    {
        var sizeX = GetReportSize(descriptor, 0x30);
        var sizeY = GetReportSize(descriptor, 0x31);

        _logicalMaxX = sizeX switch
        {
            16 => 65535,
            15 => 32767,
            _ => 16000
        };
        _logicalMaxY = sizeY switch
        {
            16 => 65535,
            15 => 32767,
            _ => 16000
        };
    }
    private static int? GetReportSize(byte[] desc, int usageId)
    {
        int? reportSize = null;
        List<ushort> usages = new();

        int i = 0;
        while (i < desc.Length)
        {
            byte prefix = desc[i++];
            if (prefix == 0xFE) { i += 2 + desc[i]; continue; }

            int sizeCode = prefix & 0x03;
            int dataSize = sizeCode == 0 ? 0 : (sizeCode == 1 ? 1 : (sizeCode == 2 ? 2 : 4));
            int type = (prefix >> 2) & 0x03;
            int tag = (prefix >> 4) & 0x0F;

            uint raw = 0;
            for (int k = 0; k < dataSize && i < desc.Length; k++)
                raw |= (uint)desc[i++] << (8 * k);

            if (type == 1 && tag == 0x7) // Report Size
            {
                reportSize = (int)raw;
            }
            else if (type == 2 && tag == 0x0) // Usage
            {
                usages.Add((ushort)raw);
            }
            else if (type == 0 && tag == 0x8) // Input
            {
                foreach (var u in usages)
                {
                    if (u == usageId) return reportSize;
                }
                usages.Clear();
            }
        }
        return null;
    }

    public int GetMaxX() => _logicalMaxX;
    public int GetMaxY() => _logicalMaxY;
    
    public override string ToString()
    {
        return $"{Manufacturer} - '{DeviceName}' (VID: {VendorId}, PID: {ProductId})";
    }

    public override int GetHashCode()
    {
        return VendorId.GetHashCode() ^ ProductId.GetHashCode();
    }
    
    public override bool Equals(object? obj)
    {
        var other = obj as TabletDevice;
        return other != null && other.VendorId == VendorId && other.ProductId == ProductId;
    }
}