namespace PxOperations.Domain.Exceptions;

public sealed class ResourceNotFoundException(string message) : Exception(message);
