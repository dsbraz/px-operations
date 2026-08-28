namespace PxOperations.Domain.Exceptions;

public sealed class BusinessStateConflictException(string message) : Exception(message);
