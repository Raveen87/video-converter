namespace VideoConverter.Api.Common.Exceptions;

public interface IApiErrorMapper
{
    object Map(Exception exception);

    int GetStatusCode(object apiError);

    IDictionary<string, object?> ToResponse(object apiError);
}
