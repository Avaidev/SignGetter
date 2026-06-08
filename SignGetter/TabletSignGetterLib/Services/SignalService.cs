using System.Runtime.InteropServices;
using TabletSignGetterLib.Exceptions;
using TabletSignGetterLib.Models;
using TabletSignGetterLib.Utilities;

namespace TabletSignGetterLib.Services
{
    public class SignalService : IDisposable
    {
        private SignalCriticalValues _criticalValues = new();
        private TabletDevice? _currentTablet;
        private ButtonsData _buttonsData = new();
        
        private IntPtr _cachedTabletHandle = IntPtr.Zero;
        private static readonly Dictionary<IntPtr, IntPtr> PreparsedHidData = new();

        private IntPtr _rawInputBuffer;
        private uint _bufferSize = 512; 
        private readonly ushort[] _hidUsageBuffer = new ushort[32];

        public bool IsBlocked { get; private set; } = true;

        public SignalService()
        {
            _rawInputBuffer = Marshal.AllocHGlobal((int)_bufferSize);
        }

        public void Reset()
        {
            _criticalValues = new();
            _buttonsData = new();
            _cachedTabletHandle = IntPtr.Zero;
        }
        
        public void ClearCache() => PreparsedHidData.Clear();

        public void Block() => IsBlocked = true;
        public void Release() => IsBlocked = false;
        
        public void SetTablet(TabletDevice tablet)
        {
            _currentTablet = tablet;
            _cachedTabletHandle = IntPtr.Zero; 
        }

        public TabletDevice? GetTablet() => _currentTablet;
        public SignalCriticalValues GetCriticalValues() => _criticalValues;

        public void SaveCriticalValues(float x, float y)
        {
            if (_criticalValues.MinX > x) _criticalValues.MinX = x;
            else if (_criticalValues.MaxX < x) _criticalValues.MaxX = x;

            if (_criticalValues.MinY > y) _criticalValues.MinY = y;
            else if (_criticalValues.MaxY < y) _criticalValues.MaxY = y;
        }
        
        public IntPtr WindowHookProcess(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg != Rih.WM_INPUT || IsBlocked) return IntPtr.Zero;
            
            if (ProcessRawInput(lParam))
            {
                handled = true;
            }
            return IntPtr.Zero;
        }

        private bool ProcessRawInput(IntPtr hRawInput)
        {
            uint size = _bufferSize;
            int headerSize = Marshal.SizeOf<Rih.RAWINPUTHEADER>();

            if (Rih.GetRawInputData(hRawInput, Rih.RID_INPUT, _rawInputBuffer, ref size, (uint)headerSize) == uint.MaxValue)
            {
                if (size > _bufferSize)
                {
                    _bufferSize = size + 128;
                    Marshal.FreeHGlobal(_rawInputBuffer);
                    _rawInputBuffer = Marshal.AllocHGlobal((int)_bufferSize);
                    return ProcessRawInput(hRawInput);
                }
                return false;
            }

            var type = Marshal.ReadInt32(_rawInputBuffer, 0);
            var hDevice = Marshal.ReadIntPtr(_rawInputBuffer, 8);

            if (!IsTargetTablet(hDevice)) return false;

            int dataOffset = IntPtr.Size == 8 ? 24 : 16;

            if (type == Rih.RIM_TYPEMOUSE)
            {
                var mouse = Marshal.PtrToStructure<Rih.RAWMOUSE>(IntPtr.Add(_rawInputBuffer, dataOffset));
                HandleMouseInput(mouse);
                return true;
            }
            else if (type == Rih.RIM_TYPEHID)
            {
                int dwSizeHid = Marshal.ReadInt32(IntPtr.Add(_rawInputBuffer, dataOffset));
                
                byte[] rawBytes = new byte[dwSizeHid];
                Marshal.Copy(IntPtr.Add(_rawInputBuffer, dataOffset + 8), rawBytes, 0, dwSizeHid);
                
                HandleHidInput(hDevice, rawBytes);
                return true;
            }

            return false;
        }

        private bool IsTargetTablet(IntPtr hDevice)
        {
            if (hDevice == _cachedTabletHandle) return true;

            var (vid, pid) = DeviceFilter.GetIdsFromPath(DeviceFilter.GetDevicePath(hDevice));
            if (vid == _currentTablet?.VendorId && pid == _currentTablet?.ProductId)
            {
                _cachedTabletHandle = hDevice;
                return true;
            }
            return false;
        }

        private void HandleMouseInput(Rih.RAWMOUSE mouse)
        {
            var raw = ParseMouseData(mouse);
            GetterManager.HandleDataReceived(CheckMouseDataButtons(raw));
        }
        
        private void HandleHidInput(IntPtr hDevice, byte[] data)
        {
            var parsed = ParseHidData(hDevice, data);
            if (parsed.X > 0 || parsed.Y > 0 || parsed.Tip)
            {
                GetterManager.HandleDataReceived(parsed);
            }
        }
        
        private TabletData CheckMouseDataButtons(TabletDataRaw raw)
        {
            _buttonsData.Tip = raw.TipPressed || (!raw.TipUnPressed && _buttonsData.Tip);
            _buttonsData.Button1 = raw.Button1Pressed || (!raw.Button1UnPressed && _buttonsData.Button1);
            _buttonsData.Button2 = raw.Button2Pressed || (!raw.Button2UnPressed && _buttonsData.Button2);

            return new TabletData
            {
                X = raw.X,
                Y = raw.Y,
                Button1 = _buttonsData.Button1,
                Button2 = _buttonsData.Button2,
                Tip = _buttonsData.Tip,
            };
        }
        
        private TabletDataRaw ParseMouseData(Rih.RAWMOUSE mouse)
        {
            if (_currentTablet == null) throw new TabletNotSelectedException();

            var data = new TabletDataRaw
            {
                X = mouse.lLastX * _currentTablet.ScaleX,
                Y = mouse.lLastY * _currentTablet.ScaleY
            };

            ushort btnData = mouse.usButtonData;
            if ((btnData & 0x0001) != 0) data.TipPressed = true;
            if (btnData == 2 || btnData == 6 || btnData == 10 || btnData == 18 || btnData == 34) data.TipUnPressed = true;
            if ((btnData & 0x0004) != 0) data.Button1Pressed = true;
            if ((btnData & 0x0008) != 0) data.Button1UnPressed = true;
            if ((btnData & 0x0010) != 0) data.Button2Pressed = true;
            if ((btnData & 0x0020) != 0) data.Button2UnPressed = true;

            return data;
        }

        private TabletData ParseHidData(IntPtr hDevice, byte[] data)
        {
            if (_currentTablet == null) return default;
            
            var pPreparsedData = GetPreparsedData(hDevice);
            if (pPreparsedData == IntPtr.Zero) return default;

            var pinnedReport = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                IntPtr pReport = pinnedReport.AddrOfPinnedObject();
                uint reportLen = (uint)data.Length;

                Rih.HidP_GetUsageValue(0, 0x01, 0, 0x30, out uint x, pPreparsedData, pReport, reportLen);
                Rih.HidP_GetUsageValue(0, 0x01, 0, 0x31, out uint y, pPreparsedData, pReport, reportLen);

                var result = new TabletData
                {
                    X = x * _currentTablet.ScaleX,
                    Y = y * _currentTablet.ScaleY
                };

                uint usageLen = (uint)_hidUsageBuffer.Length;
                Rih.HidP_GetUsages(0, 0x0D, 0, _hidUsageBuffer, ref usageLen, pPreparsedData, pReport, reportLen);
                
                for (int i = 0; i < usageLen; i++)
                {
                    switch (_hidUsageBuffer[i])
                    {
                        case 0x42: result.Tip = true; break;
                        case 0x44: result.Button1 = true; break;
                        case 0x5A: result.Button2 = true; break;
                    }
                }
                return result;
            }
            finally
            {
                pinnedReport.Free();
            }
        }

        private static IntPtr GetPreparsedData(IntPtr hDevice)
        {
            if (PreparsedHidData.TryGetValue(hDevice, out IntPtr cached)) return cached;

            uint size = 0;
            Rih.GetRawInputDeviceInfo(hDevice, Rih.RIDI_PREPARSEDDATA, IntPtr.Zero, ref size);
            if (size == 0) return IntPtr.Zero;

            IntPtr pData = Marshal.AllocHGlobal((int)size);
            Rih.GetRawInputDeviceInfo(hDevice, Rih.RIDI_PREPARSEDDATA, pData, ref size);
            
            PreparsedHidData[hDevice] = pData;
            return pData;
        }

        public void Dispose()
        {
            if (_rawInputBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_rawInputBuffer);
                _rawInputBuffer = IntPtr.Zero;
            }
        }

        ~SignalService() => Dispose();
    }
}