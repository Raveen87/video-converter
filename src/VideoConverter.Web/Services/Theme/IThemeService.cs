namespace VideoConverter.Web.Services.Theme;

public interface IThemeService
{
    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    Task<bool> IsDarkModeAsync();

    Task SetDarkModeAsync(bool isDark);

    Task ToggleThemeAsync();
}
