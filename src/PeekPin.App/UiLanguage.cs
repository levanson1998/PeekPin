namespace PeekPin;

public static class UiLanguage
{
    public const string Vietnamese = "vi";
    public const string English = "en";

    public static string Current { get; private set; } = Vietnamese;

    public static bool IsEnglish => Current == English;

    public static void Apply(string? code)
    {
        Current = string.Equals(code, English, StringComparison.OrdinalIgnoreCase) ? English : Vietnamese;
    }
}
