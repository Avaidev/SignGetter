namespace Ds510SignGetter.Exceptions;

public enum StatusCodes
{
    Success = 0,
    WindowCreationTimedOut = 1,
    OperationCanceled = 2,
    OutOfMemory = 3,
    SavingException = 4,
    CanvasIsEmpty = 5,
    WindowIsNull = 6
}

public enum SdkErrorCodes
{
    Success = 0,
    SdkNotInitialized = -1,
    DeviceOpenFailed = -2
}