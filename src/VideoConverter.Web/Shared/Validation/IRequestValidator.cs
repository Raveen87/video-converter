namespace VideoConverter.Web.Shared.Validation;

public interface IRequestValidator<T>
{
    Task<ValidationResult> ValidateAsync(T request, CancellationToken cancellationToken);
}
