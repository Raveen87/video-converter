namespace VideoConverter.Web.Services;

public class ThemeChangedEventArgs(bool isDark) : EventArgs
{
    public bool IsDark { get; } = isDark;
}
