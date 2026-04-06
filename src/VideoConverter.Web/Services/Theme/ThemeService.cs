namespace VideoConverter.Web.Services.Theme;

public class ThemeService(IJSInteropWrapper jsInterop) : IThemeService
{
    private readonly IJSInteropWrapper _jsInterop = jsInterop;
    private bool _isDarkMode;
    private bool _isInitialized;

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    public async Task<bool> IsDarkModeAsync()
    {
        if (!_isInitialized)
        {
            await InitializeAsync().ConfigureAwait(false);
        }

        return _isDarkMode;
    }

    public async Task SetDarkModeAsync(bool isDark)
    {
        if (_isDarkMode == isDark)
        {
            return;
        }

        _isDarkMode = isDark;
        await _jsInterop.InvokeVoidAsync("window.theme.setStoredTheme", [isDark]).ConfigureAwait(false);
        OnThemeChanged(isDark);
    }

    public async Task ToggleThemeAsync()
    {
        var currentMode = await IsDarkModeAsync().ConfigureAwait(false);
        await SetDarkModeAsync(!currentMode).ConfigureAwait(false);
    }

    protected virtual void OnThemeChanged(bool isDark)
    {
        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(isDark));
    }

    private async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        try
        {
            var isDark = await _jsInterop.InvokeAsync<bool>("window.theme.getInitialTheme", args: null).ConfigureAwait(false);
            _isDarkMode = isDark;
            _isInitialized = true;
        }
        catch
        {
            _isDarkMode = false;
            _isInitialized = true;
        }
    }
}
