namespace TabletLib.Models.Profile;

public class ValueRanges
{
    public int MinX { get; set; } = 0;
    public int MinY { get; set; } = 0;
    public int MinPressure { get; set; } = 0;
        
    public int MaxX { get; set; } = 32767;
    public int MaxY { get; set; } = 32767;
    public int MaxPressure { get; set; } = 1023;
        
    public int MinTilt { get; set; } = -64;
    public int MaxTilt { get; set; } = 64;
}