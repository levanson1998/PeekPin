namespace PeekPin;

public static class AppConfigNormalizer
{
    private static readonly int[] AllowedIconSizes = [32, 48, 64];

    public static AppConfig Normalize(AppConfig? source)
    {
        var config = source ?? AppConfig.CreateDefault();
        config.Version = 1;
        config.HoverDelayMs = Math.Clamp(config.HoverDelayMs, AppConfig.MinHoverDelayMs, AppConfig.MaxHoverDelayMs);
        config.HideDelayMs = Math.Clamp(config.HideDelayMs, AppConfig.MinHideDelayMs, AppConfig.MaxHideDelayMs);
        config.IconSize = NearestIconSize(config.IconSize);
        config.Language = config.Language?.Trim().ToLowerInvariant() == "en" ? "en" : "vi";
        config.Targets ??= [];
        if (config.Targets.Count > AppConfig.MaxTargets)
        {
            config.Targets = config.Targets.Take(AppConfig.MaxTargets).ToList();
        }

        foreach (var target in config.Targets)
        {
            target.ProcessName ??= "";
            target.ClassName ??= "";
            target.Title ??= "";
            target.MonitorDevice ??= "";
        }

        return config;
    }

    public static bool TryAddTarget(AppConfig config, TargetConfig target)
    {
        if (config.Targets.Count >= AppConfig.MaxTargets)
        {
            return false;
        }

        config.Targets.Add(target);
        return true;
    }

    private static int NearestIconSize(int size)
    {
        if (AllowedIconSizes.Contains(size))
        {
            return size;
        }

        return AllowedIconSizes
            .OrderBy(allowed => Math.Abs(allowed - size))
            .ThenByDescending(allowed => allowed)
            .First();
    }
}
