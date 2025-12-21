namespace VideoConverter.Common.Validation;

public interface IRequestValidator<T>
{
    Task<DomainValidationResult> ValidateAsync(T request, CancellationToken cancellationToken);
}
