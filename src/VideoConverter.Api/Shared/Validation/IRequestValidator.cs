namespace VideoConverter.Api.Shared.Validation;

public interface IRequestValidator<T>
{
    Task<DomainValidationResult> ValidateAsync(T request, CancellationToken cancellationToken);
}
