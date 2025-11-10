namespace VideoConverter.Api.Shared.Domain.ValueObjects;

public enum ConversionStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
}
