namespace TabletLib.Models.Profile;

public class ByteOffsets
{
    public FieldInfo Status { get; set; } = new FieldInfo { ByteOffset = 0, BitSize = 8 };
    public FieldInfo X { get; set; } = new FieldInfo { ByteOffset = 1, BitSize = 16 };
    public FieldInfo Y { get; set; } = new FieldInfo { ByteOffset = 3, BitSize = 16 };
    
    public FieldInfo Pressure { get; set; } = new FieldInfo { ByteOffset = 5, BitSize = 16 };
    
    public FieldInfo TiltX { get; set; } = new FieldInfo { ByteOffset = -1, BitSize = 8 };
    public FieldInfo TiltY { get; set; } = new FieldInfo { ByteOffset = -1, BitSize = 8 };
    public FieldInfo Wheel { get; set; } = new FieldInfo { ByteOffset = -1, BitSize = 8 };
}