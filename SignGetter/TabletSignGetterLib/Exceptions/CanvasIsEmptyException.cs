using TabletSignGetterLib.Models;

namespace TabletSignGetterLib.Exceptions;

public class CanvasIsEmptyException : BaseException
{
    public CanvasIsEmptyException() : base(StatusCodes.CanvasIsEmpty) {}

    public CanvasIsEmptyException(string message) : base(message, StatusCodes.CanvasIsEmpty)
    {}

    public CanvasIsEmptyException(string message, Exception inner) : base(message, inner, StatusCodes.CanvasIsEmpty)
    {}
}