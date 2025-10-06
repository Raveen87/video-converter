namespace VideoConverter.Web.Validation;

public interface IRequestValidator<T>
{
    Task<ValidationResult> ValidateAsync(T request, CancellationToken cancellationToken);
}


