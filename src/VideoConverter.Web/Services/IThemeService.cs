namespace VideoConverter.Web.Services;

public interface IThemeService
{
    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    Task<bool> IsDarkModeAsync();

    Task SetDarkModeAsync(bool isDark);

    Task ToggleThemeAsync();
}
