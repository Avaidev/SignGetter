namespace Ds510SignGetter.Models;

public struct GetterResult()
{
    private readonly object _lock = new();
    private IntPtr _resultPointer = IntPtr.Zero;
    private int _resultSize = 0;
    private int _imageWidth = 0;
    private int _imageHeight = 0;
    private int _imageStride = 0;

    public IntPtr ResultPointer
    {
        get
        {
            lock (_lock) return _resultPointer;
        }
        set
        {
            lock (_lock) _resultPointer = value;
        }
    }
    public int ResultSize
    {
        get
        {
            lock (_lock) return _resultSize;
        }
        set
        {
            lock (_lock) _resultSize = value;
        }
    }
    public int ImageWidth
    {
        get
        {
            lock (_lock) return _imageWidth;
        }
        set
        {
            lock (_lock) _imageWidth = value;
        }
    }
    public int ImageHeight
    {
        get
        {
            lock (_lock) return _imageHeight;
        }
        set
        {
            lock (_lock) _imageHeight = value;
        }
    }
    public int ImageStride
    {
        get
        {
            lock (_lock) return _imageStride;
        }
        set
        {
            lock (_lock) _imageStride = value;
        }
    }

    public void Reset()
    {
        ResultPointer = IntPtr.Zero;
        ResultSize = 0;
        ImageWidth = 0;
        ImageHeight = 0;
        ImageStride = 0;
    }
}