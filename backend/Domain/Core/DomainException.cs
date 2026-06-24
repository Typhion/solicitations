namespace Domain.Core;

public sealed class DomainException(string message) : Exception(message);