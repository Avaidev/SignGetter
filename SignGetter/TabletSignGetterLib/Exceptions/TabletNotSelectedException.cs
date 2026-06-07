using TabletSignGetterLib.Models;

namespace TabletSignGetterLib.Exceptions;

public class TabletNotSelectedException : BaseException
{
    public TabletNotSelectedException() : base(StatusCodes.TabletNotSelected) {}

    public TabletNotSelectedException(string message) : base(message, StatusCodes.TabletNotSelected)
    {}

    public TabletNotSelectedException(string message, Exception inner) : base(message, inner, StatusCodes.TabletNotSelected)
    {}
}