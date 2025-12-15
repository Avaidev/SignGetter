namespace TabletLib.Models.Profile;

public class FieldInfo
{
    public int ByteOffset { get; set; } = 0;
    public int BitSize { get; set; } = 0;
    public int BitOffset { get; set; } = 0;
    public bool IsSigned { get; set; } = false;
}