using Microsoft.JSInterop;

namespace VideoConverter.Web.Services;

public class JSInteropWrapper(IJSRuntime jsRuntime) : IJSInteropWrapper
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args, CancellationToken cancellationToken = default)
    {
        return _jsRuntime.InvokeAsync<TValue>(identifier, cancellationToken, args);
    }

    public ValueTask InvokeVoidAsync(string identifier, object?[]? args, CancellationToken cancellationToken = default)
    {
        return _jsRuntime.InvokeVoidAsync(identifier, cancellationToken, args);
    }
}
