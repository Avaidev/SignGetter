using TabletSignGetterLib.Models;

namespace TabletSignGetterLib.Exceptions;

public class InvalidInputException : BaseException
{
    public InvalidInputException() : base(StatusCodes.InvalidInput) {}

    public InvalidInputException(string message) : base(message, StatusCodes.InvalidInput)
    {}

    public InvalidInputException(string message, Exception inner) : base(message, inner, StatusCodes.InvalidInput)
    {}
}