namespace BuildingBlocks.Common.Exceptions;

public sealed class NotFoundException(string message) : Exception(message);

public sealed class ConflictException(string message) : Exception(message);

public sealed class ValidationException(IDictionary<string, string[]> errors)
    : Exception("One or more validation failures occurred.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}

public sealed class UnauthorizedException(string message = "Unauthorized.") : Exception(message);

public sealed class ForbiddenException(string message = "Forbidden.") : Exception(message);
