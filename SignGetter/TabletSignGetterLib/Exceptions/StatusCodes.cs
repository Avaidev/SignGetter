namespace TabletSignGetterLib.Exceptions;

public enum StatusCodes : int
{
    Success = 0,
    OtherException = -1,
    
    TabletListIsEmpty = 0x01,
    TabletNotFound = 0x02,
    TabletSelectionOtherException = 0x04,
    
    InvalidInput = 0x08,
    AutoSelected = 0x10,
    WindowCreationTimedOut = 0x20,
    
    SavingException = 0x40,
    CanvasIsNull = 0x80,
    CanvasIsEmpty = 0x100,
    OutOfMemory = 0x200,
    
    GetterProcessNotCompleted = 0x400,
    TabletRegisterFailed = 0x800,
    DrawingException = 0x1000,
    InputReadingException = 0x2000,
    InvalidWindow = 0x4000,
}