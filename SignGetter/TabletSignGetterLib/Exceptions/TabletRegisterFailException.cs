using TabletSignGetterLib.Models;

namespace TabletSignGetterLib.Exceptions;

public class TabletRegisterFailException : BaseException
{
    public TabletRegisterFailException() : base(StatusCodes.TabletRegisterFailed) {}

    public TabletRegisterFailException(string message) : base(message, StatusCodes.TabletRegisterFailed)
    {}

    public TabletRegisterFailException(string message, Exception inner) : base(message, inner, StatusCodes.TabletRegisterFailed)
    {}
}