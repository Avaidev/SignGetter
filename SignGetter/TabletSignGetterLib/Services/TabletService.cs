using System.Runtime.InteropServices;
using System.Text;
using HidSharp;
using Microsoft.VisualBasic;
using TabletSignGetterLib.Exceptions;
using TabletSignGetterLib.Models;
using TabletSignGetterLib.Utilities;

namespace TabletSignGetterLib.Services;

public static class TabletService
{
    private static List<TabletDevice> GetDevices()
    {
        return DeviceList.Local.GetHidDevices().Where(IsSimilarToTablet)
            .Select(d => new TabletDevice(d))
            .ToList();
    }
    
    public static TabletDevice SelectTablet()
    {
        var tablets = GetDevices();

        if (tablets.Count == 0)
            throw new TabletNotConnectedException();
        
        if (tablets.Count == 1)
            return tablets.First();
        
        var sb = new StringBuilder();
        var i = 1;
        foreach (var tablet in tablets)
        {
            sb.Append($"[{i++}] ");
            sb.Append(tablet);
            sb.Append(";\n");
        }

        sb.Append("Enter the device number:");
        var input = Interaction.InputBox(sb.ToString(), "Select Device", "1");
        if (string.IsNullOrWhiteSpace(input)) 
            throw new InvalidInputException("Enter the number");

        try
        {
            var index = Convert.ToInt32(input);
            if (index < 1 || index > tablets.Count)
                throw new InvalidInputException($"The number is not in range (1, {tablets.Count})");

            var selected = tablets[index - 1];
            return IsTabletExists(selected) ? selected : throw new TabletNotFoundException("Lost the tablet");
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            throw new InvalidInputException("Invalid format of type");
        }
    }
    
    private static bool IsSimilarToTablet(HidDevice device)
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
                "HIDI2C", "TouchPad", "Synaptics", "ELAN", "Touchscreen", "TrackPad",
                "I2C", "HID Compliant Mouse", "PS/2", "USB Input Device"
            };

            foreach (var keyword in internalKeywords)
            {
                if (device.GetProductName().ToLowerInvariant().Contains(keyword.ToLowerInvariant())) return false;
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
        Rih.RAWINPUTDEVICE[] rid = new Rih.RAWINPUTDEVICE[2];

        rid[0].usUsagePage = Rih.HID_USAGE_PAGE_GENERIC;
        rid[0].usUsage = Rih.HID_USAGE_MOUSE;
        rid[0].dwFlags = Rih.RIDEV_INPUTSINK;
        rid[0].hwndTarget = hwndTarget;

        rid[1].usUsagePage = Rih.HID_USAGE_PAGE_DIGITIZER;
        rid[1].usUsage = Rih.HID_USAGE_PEN;
        rid[1].dwFlags = Rih.RIDEV_INPUTSINK;
        rid[1].hwndTarget = hwndTarget;

        return Rih.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(typeof(Rih.RAWINPUTDEVICE)));
    }
    
    public static void UnregisterTablet()
    {
        Rih.RAWINPUTDEVICE[] rid = new Rih.RAWINPUTDEVICE[2];

        rid[0].usUsagePage = Rih.HID_USAGE_PAGE_GENERIC;
        rid[0].usUsage = Rih.HID_USAGE_MOUSE;
        rid[0].dwFlags = Rih.RIDEV_REMOVE;
        rid[0].hwndTarget = IntPtr.Zero;

        rid[1].usUsagePage = Rih.HID_USAGE_PAGE_DIGITIZER;
        rid[1].usUsage = Rih.HID_USAGE_PEN;
        rid[1].dwFlags = Rih.RIDEV_REMOVE;
        rid[1].hwndTarget = IntPtr.Zero;

        Rih.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(typeof(Rih.RAWINPUTDEVICE)));
    }
}