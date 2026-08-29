namespace PxOperations.Domain.Exceptions;

public sealed class BusinessStateConflictException : Exception
{
    public BusinessStateConflictException(string message)
        : base(message)
    {
    }

    public BusinessStateConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
