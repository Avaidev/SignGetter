using TabletSignGetterLib.Models;

namespace TabletSignGetterLib.Exceptions;

public class TabletNotFoundException : BaseException
{
    public TabletNotFoundException() : base(StatusCodes.TabletNotFound) {}

    public TabletNotFoundException(string message) : base(message, StatusCodes.TabletNotFound)
    {}

    public TabletNotFoundException(string message, Exception inner) : base(message, inner, StatusCodes.TabletNotFound)
    {}
}