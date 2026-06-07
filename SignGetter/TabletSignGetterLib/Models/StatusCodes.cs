namespace TabletSignGetterLib.Models;

public enum StatusCodes
{
    Success = 0x0,
    Interrupted = 0x1,
    OtherException = 0x2,
    
    NoTabletsFound = 0x3,
    TabletNotFound = 0x4,
    TabletNotSelected = 0x5,
    
    TabletRegisterFailed = 0x6,
    
    WindowStartTimeOut = 0x7,
    WindowNotFound = 0x8,
    CanvasIsEmpty = 0x9,
    
    InvalidInput = 0xA,
    
    IsExecuting = 0xB,
}