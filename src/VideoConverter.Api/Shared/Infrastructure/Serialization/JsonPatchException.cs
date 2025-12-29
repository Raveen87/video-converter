namespace VideoConverter.Api.Shared.Infrastructure.Serialization;

public class JsonPatchException : Exception
{
    public JsonPatchException(string message)
        : base(message)
    {
    }

    public JsonPatchException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
