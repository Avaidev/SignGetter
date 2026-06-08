namespace TabletSignGetterLib.Models;

public struct TabletDataRaw()
{
    public float X = 0f;
    public float Y = 0f;
    public bool TipPressed = false;
    public bool TipUnPressed = false;
    public bool Button1Pressed = false;
    public bool Button1UnPressed = false;
    public bool Button2Pressed = false;
    public bool Button2UnPressed = false;
}

public struct TabletData()
{
    public float X = 0f;
    public float Y = 0f;
    public bool Tip = false;
    public bool Button1 = false;
    public bool Button2 = false;
}