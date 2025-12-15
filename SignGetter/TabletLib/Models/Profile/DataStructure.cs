namespace TabletLib.Models.Profile;

public class DataStructure
{
    public int PacketSize { get; set; } = 8;
    public ByteOffsets ByteOffsets { get; set; } = new ByteOffsets();
    public ValueRanges ValueRanges { get; set; } = new ValueRanges();
    public StatusMasks StatusMasks { get; set; } = new StatusMasks();
}