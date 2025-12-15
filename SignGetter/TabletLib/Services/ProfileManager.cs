using System.Text.Json;
using HidSharp;
using HidSharp.Reports;
using TabletLib.Models;
using TabletLib.Models.Profile;
using TabletLib.Utilities;

namespace TabletLib.Services;

internal static class ProfileManager
{
    public static string ProfilesFileName = "profiles.json";
    
    #region Generating Profile
    public static TabletProfile GenerateProfile(HidDevice device)
    {
        var profile = new TabletProfile
        {
            DeviceName = device.GetProductName() ?? "Unknown Tablet",
            Manufacturer = device.GetManufacturer() ?? "Unknown Manufacturer",
            VendorId = device.VendorID,
            ProductId = device.ProductID,
            DataFormat = "AutoDetect"
        };
        
        var descriptor = device.GetReportDescriptor();
        if (descriptor == null)
        {
            Console.WriteLine("[ProfileManager] Failed to load descriptor of device");
            return GetDefaultProfile(profile);
        }

        var inputReport = descriptor.InputReports.OrderByDescending(r => r.Length).FirstOrDefault();
        if (inputReport == null)
        {
            Console.WriteLine("[ProfileManager] Failed to find input report");
            return GetDefaultProfile(profile);
        }
        
        profile.Data.PacketSize = inputReport.Length;

        var analysis = AnalyzeDataItem(inputReport.DataItems);
        
        CompleteProfile(analysis, profile);
        ValidateAndCreateProfile(profile);

        AddProfile(profile);
        return profile;
    }

    private static DataItemAnalysis AnalyzeDataItem(IEnumerable<DataItem> dataItems)
    {
        var analysis = new  DataItemAnalysis();
        var bitPosition = 0;

        foreach (var dataItem in dataItems)
        {
            var itemInfo = new DataItemInfo
            {
                DataItem = dataItem,
                StartBit = bitPosition,
                EndBit = bitPosition + dataItem.TotalBits - 1,
                ByteOffset = bitPosition / 8,
                BitOffset = bitPosition % 8,
                FieldType = ClassifyDataItem(dataItem)
            };

            analysis.Items.Add(itemInfo);
            if (dataItem.LogicalMaximum > analysis.MaxLogicalValue) analysis.MaxLogicalValue = dataItem.LogicalMaximum;
            analysis.TotalBits += dataItem.TotalBits;
            
            switch (itemInfo.FieldType)
            {
                case FieldType.Coordinate:
                    analysis.CoordinateFields.Add(itemInfo);
                    break;
                case FieldType.Pressure:
                    analysis.PressureFields.Add(itemInfo);
                    break;
                case FieldType.SignedValue:
                case FieldType.PushButton:
                    analysis.ButtonFields.Add(itemInfo);
                    break;
            }

            bitPosition += dataItem.TotalBits;
        }

        return analysis;
    }

    private static void CompleteProfile(DataItemAnalysis analysis, TabletProfile profile)
    {
        if (analysis.CoordinateFields.Count >= 2)
        {
            var x = analysis.CoordinateFields[0];
            var y = analysis.CoordinateFields[1];

            profile.Data.ByteOffsets.X = x.ToFieldInfo();
            profile.Data.ByteOffsets.Y = y.ToFieldInfo();

            profile.Data.ValueRanges.MaxX = x.DataItem.LogicalMaximum;
            profile.Data.ValueRanges.MaxY = y.DataItem.LogicalMaximum;
        }

        if (analysis.PressureFields.Count > 0)
        {
            var pressure = analysis.PressureFields[0];

            profile.Data.ByteOffsets.Pressure = pressure.ToFieldInfo();
            profile.Data.ValueRanges.MaxPressure = pressure.DataItem.LogicalMaximum;
        }

        var flagIndex = 0;

        foreach (var btn in analysis.ButtonFields.OrderBy(b => b.StartBit))
        {
            switch (flagIndex)
            {
                case 0:
                    profile.Data.StatusMasks.TipSwitch = btn.BitOffset;
                    break;
                case 1:
                    profile.Data.StatusMasks.InRange = btn.BitOffset;
                    break;
                case 5:
                    profile.Data.StatusMasks.Eraser = btn.BitOffset;
                    break;
                case 6:
                    profile.Data.StatusMasks.BarrelButton1 = btn.BitOffset;
                    break;
                case 7:
                    profile.Data.StatusMasks.BarrelButton2 = btn.BitOffset;
                    break;
            }
            
            flagIndex++;
        }
    }

    private static void ValidateAndCreateProfile(TabletProfile profile)
    {
        var hasX = profile.Data.ByteOffsets.X.ByteOffset >= 0;
        var hasY = profile.Data.ByteOffsets.Y.ByteOffset >= 0;
        var hasPressure = profile.Data.ByteOffsets.Pressure.ByteOffset >= 0;
        
        if (!hasX || !hasY)
        {
            Console.WriteLine("[ProfileManager] Failed to find coordinates. Setting default");
            
            if (!hasX)
            {
                profile.Data.ByteOffsets.X = new FieldInfo { ByteOffset = 1, BitSize = 16 };
                profile.Data.ValueRanges.MaxX = 32767;
            }
            
            if (!hasY)
            {
                profile.Data.ByteOffsets.Y = new FieldInfo { ByteOffset = 3, BitSize = 16 };
                profile.Data.ValueRanges.MaxY = 32767;
            }
        }
        
        if (!hasPressure)
        {
            Console.WriteLine("[ProfileManager] Failed to find pressure. Setting default");
            profile.Data.ByteOffsets.Pressure = new FieldInfo { ByteOffset = 5, BitSize = 10 };
            profile.Data.ValueRanges.MaxPressure = 1023;
        }
    }
    
    private static FieldType ClassifyDataItem(DataItem dataItem)
    {
        if (dataItem.IsConstant) return FieldType.Constant;
        if (dataItem.IsBoolean && dataItem.ElementBits == 1)
        {
            if (dataItem.IsAbsolute) return dataItem.HasPreferredState ? FieldType.PushButton : FieldType.ToggleButton;
            else if (dataItem.IsRelative) return FieldType.OneShot;
        }
        else if (dataItem.IsVariable)
        {
            if (dataItem.ElementBits >= 8)
            {
                if (dataItem.LogicalMaximum >= 1000 && dataItem.LogicalMaximum <= 65535)
                {
                    return FieldType.Coordinate;
                }
                else if (dataItem.LogicalMaximum >= 255 && dataItem.LogicalMaximum <= 8191)
                {
                    return FieldType.Pressure;
                }
                else if (dataItem.IsLogicalSigned && 
                         Math.Abs(dataItem.LogicalMinimum) == dataItem.LogicalMaximum &&
                         dataItem.LogicalMaximum <= 127)
                {
                    return FieldType.SignedValue;
                }
            }
        }

        return FieldType.Unknown;
    }
    #endregion

    public static IList<TabletProfile>? GetAllProfiles()
    {
        var fullPath = Utils.GetFullPath("Data");
        var filePath = Path.Combine(fullPath, ProfilesFileName);
        if (string.IsNullOrEmpty(fullPath)
            || !File.Exists(filePath)) return null;

        List<TabletProfile>? profiles;
        try
        {
            var json = File.ReadAllText(filePath);
            profiles = JsonSerializer.Deserialize<List<TabletProfile>>(json) ?? new();
            Console.WriteLine("[ProfileManager] Loaded profiles: {0}",  profiles.Count);
        }
        catch (Exception ex)
        {
            profiles = null;
            Console.WriteLine("[ProfileManager] LoadAllProfiles: Error loading profiles - {0}]", ex.Message);
        }

        return profiles;
    }

    private static bool SaveAllProfiles(List<TabletProfile> profiles)
    {
        var fullPath = Utils.GetFullPath("Data");
        var filePath = Path.Combine(fullPath, ProfilesFileName);
        if (string.IsNullOrEmpty(fullPath)
            || !File.Exists(filePath)) return false;

        try
        {
            var json = JsonSerializer.Serialize(profiles);
            File.WriteAllText(filePath, json);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ProfileManager] SaveAllProfiles: Error saving profiles - {0}", ex.Message);
            return false;
        }
    }
    
    public static TabletProfile? GetProfile(HidDevice device)
    {
        var profiles = GetAllProfiles();
        return profiles?.FirstOrDefault(p => p.VendorId == device.VendorID && p.ProductId == device.ProductID);
    }

    public static bool AddProfile(TabletProfile profile)
    {
        var profiles = GetAllProfiles();
        if (profiles == null) return false;
        profiles.Add(profile);
        return SaveAllProfiles(profiles.ToList());
    }

    private static TabletProfile GetDefaultProfile(TabletProfile baseProfile)
    {
        return new TabletProfile
        {
            DeviceName = baseProfile.DeviceName,
            Manufacturer = baseProfile.Manufacturer,
            VendorId = baseProfile.VendorId,
            ProductId = baseProfile.ProductId,
            DataFormat = "Default"
        };
    }

    sealed class DataItemAnalysis
    {
        public List<DataItemInfo> Items { get; } = new();
        public List<DataItemInfo> CoordinateFields { get; } = new();
        public List<DataItemInfo> PressureFields { get; } = new();
        public List<DataItemInfo> ButtonFields { get; } = new();
        public int TotalBits { get; set; }
        public int MaxLogicalValue { get; set; }
    }

    sealed class DataItemInfo
    {
        public DataItem DataItem { get; set; }
        public FieldType FieldType { get; set; }
        public int StartBit { get; set; }
        public int EndBit { get; set; }
        public int ByteOffset { get; set; }
        public int BitOffset { get; set; }

        public FieldInfo ToFieldInfo()
        {
            return new FieldInfo
            {
                BitOffset = BitOffset,
                ByteOffset = ByteOffset,
                BitSize = DataItem.ElementBits,
                IsSigned = DataItem.IsLogicalSigned
            };
        }
    }
}