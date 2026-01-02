using TabletSignGetterLib.Exceptions;

namespace TabletSignGetterLib.Models;

public struct GetterStatus()
{
    public bool IsBlocked = false;
    public bool IsRegistered = false;
    public bool TipStatus = false;
    public bool IsExecuting = false;

    public int StatusCode = (int)StatusCodes.Success; 

    public float MinX = float.MaxValue;
    public float MinY = float.MaxValue;
    public float MaxX = 0;
    public float MaxY = 0;
}