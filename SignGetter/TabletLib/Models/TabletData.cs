namespace TabletLib.Models;

public class TabletData
{
    public byte X { get; set; }
    public byte Y { get; set; }
    public sbyte TiltX { get; set; }
    public sbyte TiltY { get; set; }
    public sbyte Wheel { get; set;}
    public ushort Pressure { get; set; }
    public bool IsPenDown { get; set; }
    public bool InRange { get; set; }
    public bool IsEraser { get; set; }
    public bool Button1 { get; set; }
    public bool Button2 { get; set; }
}