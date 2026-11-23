namespace StoreApi.Domain.Exceptions;

/// <summary>Raised when a domain invariant would be violated.</summary>
public class DomainException(string message) : Exception(message);
