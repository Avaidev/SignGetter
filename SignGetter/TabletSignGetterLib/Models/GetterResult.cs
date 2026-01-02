namespace TabletSignGetterLib.Models;

public struct GetterResult()
{
    public IntPtr ResultPointer = IntPtr.Zero;
    public int ResultSize = 0;

    public int ImageWidth = 0;
    public int ImageHeight = 0;
    public int ImageStride = 0;

    public void Reset()
    {
        ResultPointer = IntPtr.Zero;
        ResultSize = 0;
        ImageWidth = 0;
        ImageHeight = 0;
        ImageStride = 0;
    }
}