namespace TabletSignGetterLib.Exceptions;

public class InvalidWindowException : Exception
{
    public InvalidWindowException() : base() { }
    public InvalidWindowException(string message) : base(message){}
    public InvalidWindowException(string message, Exception inner) : base(message, inner) { }
}