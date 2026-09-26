using System.Resources;

namespace PeekPin;

public static class Strings
{
    private static readonly ResourceManager Resources = new("PeekPin.Strings", typeof(Strings).Assembly);

    public static string SelectWindowPrompt => Get(nameof(SelectWindowPrompt));
    public static string InaccessibleStatus => Get(nameof(InaccessibleStatus));
    public static string Dock => Get(nameof(Dock));
    public static string Unwatch => Get(nameof(Unwatch));
    public static string AddWindow => Get(nameof(AddWindow));
    public static string ShowAndKeep => Get(nameof(ShowAndKeep));
    public static string Minimize => Get(nameof(Minimize));
    public static string Settings => Get(nameof(Settings));
    public static string Pause => Get(nameof(Pause));
    public static string Resume => Get(nameof(Resume));
    public static string Quit => Get(nameof(Quit));
    public static string PhaseVisible => Get(nameof(PhaseVisible));
    public static string PhaseDocked => Get(nameof(PhaseDocked));
    public static string PhasePeeking => Get(nameof(PhasePeeking));
    public static string PhaseUnbound => Get(nameof(PhaseUnbound));
    public static string PhaseInaccessible => Get(nameof(PhaseInaccessible));
    public static string HoverDelay => Get(nameof(HoverDelay));
    public static string HideDelay => Get(nameof(HideDelay));
    public static string IconSize => Get(nameof(IconSize));
    public static string RememberPosition => Get(nameof(RememberPosition));
    public static string StartWithWindows => Get(nameof(StartWithWindows));
    public static string HideIconWhenFullscreen => Get(nameof(HideIconWhenFullscreen));
    public static string WatchedTitle => Get(nameof(WatchedTitle));
    public static string CatalogTitle => Get(nameof(CatalogTitle));
    public static string AppTitle => Get(nameof(AppTitle));
    public static string DeveloperName => Get(nameof(DeveloperName));
    public static string Back => Get(nameof(Back));
    public static string Save => Get(nameof(Save));
    public static string NoWindows => Get(nameof(NoWindows));
    public static string Language => Get(nameof(Language));

    public static string PhaseName(SessionPhase phase) => phase switch
    {
        SessionPhase.Visible => PhaseVisible,
        SessionPhase.Docked => PhaseDocked,
        SessionPhase.Peeking => PhasePeeking,
        SessionPhase.Inaccessible => PhaseInaccessible,
        _ => PhaseUnbound
    };

    private static readonly ResourceManager English = new("PeekPin.Strings.en", typeof(Strings).Assembly);

    private static string Get(string key)
    {
        if (UiLanguage.IsEnglish)
        {
            var english = English.GetString(key, System.Globalization.CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(english))
            {
                return english;
            }
        }

        return Resources.GetString(key) ?? key;
    }
}
