namespace ChatService.Services.Exceptions;

/// <summary>
/// Thrown when a requested resource is not found.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string resourceName, object key)
        : base($"Resource '{resourceName}' with identifier '{key}' was not found.")
    {
    }
}
