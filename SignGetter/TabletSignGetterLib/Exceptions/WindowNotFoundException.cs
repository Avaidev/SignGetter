using TabletSignGetterLib.Models;

namespace TabletSignGetterLib.Exceptions;

public class WindowNotFoundException : BaseException
{
    public WindowNotFoundException() : base(StatusCodes.WindowNotFound) {}

    public WindowNotFoundException(string message) : base(message, StatusCodes.WindowNotFound)
    {}

    public WindowNotFoundException(string message, Exception inner) : base(message, inner, StatusCodes.WindowNotFound)
    {}
}