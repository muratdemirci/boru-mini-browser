using Avalonia;
using Avalonia.Styling;
using MiniBrowser.Core.Models;

namespace MiniBrowser.Services.Settings;

/// <summary>Applies the selected theme to the running Avalonia application.</summary>
public interface IThemeService
{
    /// <summary>Applies <paramref name="preference"/> to the current application.</summary>
    void Apply(ThemePreference preference);
}

/// <summary>
/// Maps a <see cref="ThemePreference"/> onto Avalonia's <c>RequestedThemeVariant</c>.
/// The default variant follows the OS theme.
/// </summary>
public sealed class ThemeService : IThemeService
{
    public void Apply(ThemePreference preference)
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.RequestedThemeVariant = preference switch
        {
            ThemePreference.Light => ThemeVariant.Light,
            ThemePreference.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }
}
