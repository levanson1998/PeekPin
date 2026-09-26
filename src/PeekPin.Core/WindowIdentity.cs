namespace PeekPin;

public readonly record struct WindowIdentity(string ProcessName, string ClassName, string Title)
{
    public bool Matches(WindowIdentity other)
    {
        return string.Equals(ProcessName, other.ProcessName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(ClassName, other.ClassName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Title, other.Title, StringComparison.OrdinalIgnoreCase);
    }
}

public readonly record struct WindowCandidate(WindowId Id, WindowIdentity Identity);

public static class WindowIdentityMatcher
{
    public static WindowId?[] Match(IReadOnlyList<TargetConfig> targets, IReadOnlyList<WindowCandidate> windows)
    {
        var result = new WindowId?[targets.Count];
        var used = new HashSet<nint>();
        for (var i = 0; i < targets.Count; i++)
        {
            var wanted = new WindowIdentity(targets[i].ProcessName, targets[i].ClassName, targets[i].Title);
            foreach (var window in windows)
            {
                if (used.Contains(window.Id.Value))
                {
                    continue;
                }

                if (!wanted.Matches(window.Identity))
                {
                    continue;
                }

                result[i] = window.Id;
                used.Add(window.Id.Value);
                break;
            }
        }

        return result;
    }
}
