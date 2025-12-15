namespace TabletLib.Models.Profile;

public class StatusMasks
{
    // Bits in status byte
    public int TipSwitch { get; set; } = 0;
    public int InRange { get; set; } = 1;
    public int Eraser { get; set; } = 5;
    public int BarrelButton1 { get; set; } = 6;
    public int BarrelButton2 { get; set; } = 7;
}