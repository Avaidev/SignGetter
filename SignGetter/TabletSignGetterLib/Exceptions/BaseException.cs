using TabletSignGetterLib.Models;

namespace TabletSignGetterLib.Exceptions;

public class BaseException : Exception
{
    public StatusCodes StatusCode { get; private set; }

    public BaseException(StatusCodes statusCode) : base()
    {
        StatusCode = statusCode;
    }

    public BaseException(string message, StatusCodes statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    public BaseException(string message, Exception inner, StatusCodes statusCode) : base(message, inner)
    {
        StatusCode = statusCode;
    }
}