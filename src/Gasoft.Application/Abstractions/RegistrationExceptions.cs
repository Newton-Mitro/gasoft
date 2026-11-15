namespace Gasoft.Application.Abstractions;

public sealed class RegistrationConflictException(string message) : Exception(message);

public sealed class RegistrationValidationException(
    IReadOnlyDictionary<string, string[]> errors)
    : Exception("The registration request is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
