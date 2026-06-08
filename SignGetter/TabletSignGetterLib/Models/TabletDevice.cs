using HidSharp;

namespace TabletSignGetterLib.Models
{
    public class TabletDevice
    {
        public enum ModeVariants
        {
            Low = 0 ,
            Medium = 1,
            High = 2,
            Custom = 3
        }
        
        private int[] _maxXVariants = [16000, 32767, 65535, 1];
        private int[] _maxYVariants = [16000, 32767, 65535, 1];
        
        public string DeviceName { get; private set; }
        public string Manufacturer { get; private set; }
        public readonly int VendorId;
        public readonly int ProductId;

        private ModeVariants _currentVariant = ModeVariants.Custom;

        public float ScaleX { get; private set; }
        public float ScaleY { get; private set; }

        public TabletDevice(HidDevice device)
        {
            DeviceName = device.GetProductName() ?? "HID Tablet";
            Manufacturer = device.GetManufacturer() ?? "Unknown";
            VendorId = device.VendorID;
            ProductId = device.ProductID;

            byte[] descriptor = device.GetRawReportDescriptor();
            ParseDescriptor(descriptor);
            
            UpdateScaleX();
            UpdateScaleY();
        }

        public void ChangeMode(ModeVariants mode)
        {
            _currentVariant = mode;
            UpdateScaleX();
            UpdateScaleY();
        }

        public ModeVariants SetNextMode()
        {
            var newMode = _currentVariant == ModeVariants.Custom ? ModeVariants.Low : _currentVariant + 1;
            ChangeMode(newMode);
            return newMode;
        }
        private void UpdateScaleX()
            => ScaleX = 1.0f / _maxXVariants[(int)_currentVariant];
        
        private void UpdateScaleY()
            => ScaleY = 1.0f / _maxYVariants[(int)_currentVariant];

        private void ParseDescriptor(byte[] desc)
        {
            var xInfo = GetUsageInfo(desc, new[] { 0x01, 0x0D }, 0x30);
            var yInfo = GetUsageInfo(desc, new[] { 0x01, 0x0D }, 0x31);

            _maxXVariants[(int)ModeVariants.Custom] = ResolveResolution(xInfo);
            _maxYVariants[(int)ModeVariants.Custom] = ResolveResolution(yInfo);
        }

        private int ResolveResolution(HidUsageResult result)
        {
            if (result.LogicalMax > 16000)
            {
                return result.LogicalMax;
            }

            if (result.ReportSize > 0)
            {
                return result.ReportSize switch
                {
                    15 => 32767,
                    16 => 65535,
                    _ => result.LogicalMax > 0 ? result.LogicalMax : 16000
                };
            }

            return result.LogicalMax > 0 ? result.LogicalMax : 32767;
        }

        private struct HidUsageResult { public int LogicalMax; public int ReportSize; }

        private static HidUsageResult GetUsageInfo(byte[] desc, int[] targetPages, int usageId)
        {
            int currentUsagePage = 0;
            int lastLogicalMax = 0;
            int lastReportSize = 0;
            List<int> usages = new List<int>();

            int i = 0;
            while (i < desc.Length)
            {
                byte prefix = desc[i++];
                
                // Handle Long Items (First class logic)
                if (prefix == 0xFE) { i += 2 + desc[i]; continue; }

                int sizeCode = prefix & 0x03;
                int dataSize = sizeCode == 3 ? 4 : sizeCode;
                int type = (prefix >> 2) & 0x03;
                int tag = (prefix >> 4) & 0x0F;

                uint raw = 0;
                for (int k = 0; k < dataSize && i < desc.Length; k++)
                    raw |= (uint)desc[i++] << (8 * k);

                if (type == 1) // Global Item
                {
                    if (tag == 0x0) currentUsagePage = (int)raw;
                    if (tag == 0x2) lastLogicalMax = (int)raw;
                    if (tag == 0x7) lastReportSize = (int)raw;
                }
                else if (type == 2 && tag == 0x0) // Local Item: Usage
                {
                    usages.Add((int)raw);
                }
                else if (type == 0 && tag == 0x8) // Main Item: Input
                {
                    if (usages.Contains(usageId) && targetPages.Contains(currentUsagePage))
                    {
                        return new HidUsageResult { 
                            LogicalMax = lastLogicalMax, 
                            ReportSize = lastReportSize 
                        };
                    }
                    usages.Clear();
                }
                else if (type == 0 && (tag == 0xA || tag == 0xC))
                {
                    usages.Clear();
                }
            }
            return new HidUsageResult { LogicalMax = 0, ReportSize = 0 };
        }
    }
}