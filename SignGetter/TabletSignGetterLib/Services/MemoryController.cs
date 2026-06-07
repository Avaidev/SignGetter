using System.Runtime.InteropServices;

namespace TabletSignGetterLib.Services;

public class MemoryController
{
    private readonly LinkedList<IntPtr> _usingPointers = new();
    private readonly object _lock = new object();

    public IntPtr AllocateNew(int size)
    {
        var ptr = Marshal.AllocHGlobal(size);
        lock (_lock)
        {
            _usingPointers.AddLast(ptr);
        }
        return ptr;
    }

    public void Free()
    {
        IntPtr? ptr;
        lock (_lock)
        {
            ptr = _usingPointers.First?.Value;
            if (!ptr.HasValue) return;
            
            _usingPointers.RemoveFirst();
        }
        Marshal.FreeHGlobal(ptr.Value);
    }

    public void FreeAll()
    {
        lock (_lock)
        {
            foreach (var ptr in _usingPointers)
            {
                Marshal.FreeHGlobal(ptr);
            }
            _usingPointers.Clear();
        }
    }
}