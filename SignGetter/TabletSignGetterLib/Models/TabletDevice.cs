using HidSharp;

namespace TabletSignGetterLib.Models
{
    public class TabletDevice
    {
        public string DeviceName { get; private set; }
        public string Manufacturer { get; private set; }
        public readonly int VendorId;
        public readonly int ProductId;

        public float ScaleX { get; private set; }
        public float ScaleY { get; private set; }
        public float ScalePressure { get; private set; }

        private int _maxX;
        private int _maxY;
        private int _maxPressure;

        public TabletDevice(HidDevice device)
        {
            DeviceName = device.GetProductName();
            Manufacturer = device.GetManufacturer();
            VendorId = device.VendorID;
            ProductId = device.ProductID;

            ParseHidDescriptor(device.GetRawReportDescriptor());
            
            ScaleX = _maxX > 0 ? 1.0f / _maxX : 1.0f;
            ScaleY = _maxY > 0 ? 1.0f / _maxY : 1.0f;
            ScalePressure = _maxPressure > 0 ? 1.0f / _maxPressure : 1.0f;
        }

        private void ParseHidDescriptor(byte[] desc)
        {
            _maxX = GetLogicalMax(desc, 0x01, 0x30) ?? 32767;
            _maxY = GetLogicalMax(desc, 0x01, 0x31) ?? 32767;
            _maxPressure = GetLogicalMax(desc, 0x0D, 0x30) ?? 8191;
        }

        private static int? GetLogicalMax(byte[] desc, int usagePage, int usageId)
        {
            int currentUsagePage = 0;
            int lastLogicalMax = 0;
            List<int> currentUsages = new List<int>();

            int i = 0;
            while (i < desc.Length)
            {
                byte prefix = desc[i++];
                int sizeCode = prefix & 0x03;
                int dataSize = sizeCode == 3 ? 4 : sizeCode;
                int type = (prefix >> 2) & 0x03;
                int tag = (prefix >> 4) & 0x0F;

                uint raw = 0;
                for (int k = 0; k < dataSize && i < desc.Length; k++)
                    raw |= (uint)desc[i++] << (8 * k);

                if (type == 1) // Global Item
                {
                    if (tag == 0x0) currentUsagePage = (int)raw; // Usage Page
                    if (tag == 0x2) lastLogicalMax = (int)raw;   // Logical Maximum
                }
                else if (type == 2) // Local Item
                {
                    if (tag == 0x0) currentUsages.Add((int)raw); // Usage
                }
                else if (type == 0 && tag == 0x8) // Input Main Item
                {
                    if (currentUsagePage == usagePage && currentUsages.Contains(usageId))
                        return lastLogicalMax;
                    currentUsages.Clear();
                }
            }
            return null;
        }

        public int GetMaxX() => _maxX;
        public int GetMaxY() => _maxY;
        public int GetMaxPressure() => _maxPressure;
    }
}