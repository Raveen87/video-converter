namespace VideoConverter.Web.Services;

#pragma warning disable MA0109 // Consider adding an overload with a Span<T> or Memory<T>
public interface IJSInteropWrapper
{
    ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args, CancellationToken cancellationToken = default);

    ValueTask InvokeVoidAsync(string identifier, object?[]? args, CancellationToken cancellationToken = default);
}
#pragma warning restore MA0109
