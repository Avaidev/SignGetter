using TabletSignGetterLib.Models;

namespace TabletSignGetterLib.Exceptions;

public class TabletNotConnectedException : BaseException
{
    public TabletNotConnectedException() : base(StatusCodes.NoTabletsFound) {}

    public TabletNotConnectedException(string message) : base(message, StatusCodes.NoTabletsFound)
    {}

    public TabletNotConnectedException(string message, Exception inner) : base(message, inner, StatusCodes.NoTabletsFound)
    {}
}