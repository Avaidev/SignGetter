using Ds510SignGetter.Exceptions;

namespace Ds510SignGetter.Models;

public struct GetterStatus()
{
    private readonly object _lock = new();
    private bool _isExecuting = false;
    private bool _initialized = false;
    private int _statusCode = (int)StatusCodes.Success;

    public bool IsExecuting
    {
        get
        {
            lock (_lock) return _isExecuting;
        }
        set
        {
            lock (_lock) _isExecuting = value;
        }
    }
    public bool Initialized
    {
        get
        {
            lock (_lock) return _initialized;
        }
        set
        {
            lock(_lock) _initialized = value;
        }
    }
    public int StatusCode
    {
        get
        {
            lock (_lock) return _statusCode;
        }
        set
        {
            lock (_lock) _statusCode = value;
        }
    }

    public void Reset()
    { 
        StatusCode = (int)StatusCodes.Success;
    }
}