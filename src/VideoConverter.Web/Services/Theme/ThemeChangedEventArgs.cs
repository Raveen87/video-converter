namespace VideoConverter.Web.Services.Theme;

public class ThemeChangedEventArgs(bool isDark) : EventArgs
{
    public bool IsDark { get; } = isDark;
}
