using System.Runtime.InteropServices;
using System.Text;
using HidSharp;
using Microsoft.VisualBasic;
using TabletSignGetterLib.Models;
using TabletSignGetterLib.Utilities;

namespace TabletSignGetterLib.Manager;

public static class TabletManager
{
    public static List<TabletDevice> GetDevices()
    {
        return DeviceList.Local.GetHidDevices().Where(IsSimilarToTablet)
            .Select(d => new TabletDevice(d)).ToList();
    }

    public static TabletDevice? SelectTablet()
    {
        var tablets1 = GetDevices();
        if (tablets1.Count == 0)
        {
            Console.WriteLine("[TabletManager] The tablets list is empty");
            MessageService.ErrorMessage("The tablets list is empty");
            return null;
        }
        if (tablets1.Count == 1) return tablets1.First();
        var sb = new StringBuilder();
        var i = 1;
        foreach (var tablet in tablets1)
        {
            sb.Append($"[{i++}] ");
            sb.Append(tablet);
            sb.Append(";\n");
        }

        sb.Append("Enter the device number:");
        var input = Interaction.InputBox(sb.ToString(), "Select Device", "1");
        if (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input)) return null;
        try
        {
            var index = Convert.ToInt32(input);
            if (index < 1 || index > tablets1.Count) return null;
            var selected = tablets1[index - 1];
            return IsTabletExists(selected) ? selected : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[TabletManager] Exception in selecting tablet input: {0}]", ex.Message);
            return null;
        }
    }
    
    public static bool IsSimilarToTablet(HidDevice device)
    {
        try
        {
            bool isDigitizer = device.GetReportDescriptor() != null
                               && device.GetReportDescriptor().DeviceItems
                                   .SelectMany(item => item.Usages.GetAllValues())
                                   .Any(usage => (ushort)(usage >> 16) == 0x0D);

            if (!isDigitizer) return false;

            string[] internalKeywords =
            {
                "HIDI2C", "TouchPad", "Touchpad", "Synaptics", "ELAN",
                "Precision Touchpad", "Touchscreen", "TrackPad", "Trackpad",
                "I2C", "HID Compliant Mouse", "PS/2", "USB Input Device"
            };

            foreach (var keyword in internalKeywords)
            {
                if (device.GetProductName().Contains(keyword, StringComparison.OrdinalIgnoreCase)) return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[TabletManager > IsSimilarToTablet] Exception with VID:0x{0:x} PID:0x{1:x}: {2}", device.VendorID, device.ProductID, ex.Message);
            return false;
        }
    }

    public static bool IsTabletExists(TabletDevice tablet) => GetDevices().Contains(tablet);

    public static bool RegisterTablet(IntPtr hwndTarget)
    {
        var rid = new Rih.RAWINPUTDEVICE
        {
            usUsagePage = Rih.HID_USAGE_PAGE_GENERIC,
            usUsage = Rih.HID_USAGE_PEN,
            dwFlags = Rih.RIDEV_INPUTSINK,
            hwndTarget = hwndTarget
        };

        return Rih.RegisterRawInputDevices([rid], 1, (uint)Marshal.SizeOf(typeof(Rih.RAWINPUTDEVICE)));
    }
    
    public static void UnregisterTablet()
    {
        var rid = new Rih.RAWINPUTDEVICE
        {
            usUsagePage = Rih.HID_USAGE_PAGE_GENERIC,
            usUsage = Rih.HID_USAGE_PEN,
            dwFlags = Rih.RIDEV_REMOVE,
            hwndTarget = IntPtr.Zero
        };

        Rih.RegisterRawInputDevices([rid], 1, (uint)Marshal.SizeOf(typeof(Rih.RAWINPUTDEVICE)));
    }
}