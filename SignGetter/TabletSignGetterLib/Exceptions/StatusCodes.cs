namespace TabletSignGetterLib.Exceptions;

public enum StatusCodes : int
{
    Success = 0,
    TabletNotFoundDuringSelection = 0x01,
    GetterProcessNotCompleted = 0x02,
    CanvasWindowTimedOut = 0x04,
    TabletRegisterFailed = 0x08,
    CanvasIsNull = 0x10,
    OutOfMemory = 0x20,
    CanvasIsEmpty = 0x40,
    DrawingException = 0x80,
    InputReadingException = 0x100,
    OtherException = 0x8000,
}