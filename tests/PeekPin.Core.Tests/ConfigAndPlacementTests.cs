using System.IO;
using PeekPin;

namespace PeekPin.Core.Tests;

public class ConfigAndPlacementTests
{
    [Fact]
    public void Config_RoundTrip_AndClamps()
    {
        var directory = Path.Combine(Path.GetTempPath(), "peekpin-tests", Guid.NewGuid().ToString("N"));
        var store = new AppConfigStore(directory);
        var config = new AppConfig
        {
            HoverDelayMs = 0,
            IconSize = 40,
            Targets = [new TargetConfig { ProcessName = "notepad", ClassName = "Notepad", Title = "Untitled" }]
        };
        store.Save(config);
        var loaded = store.Load();
        Assert.Equal(100, loaded.HoverDelayMs);
        Assert.Equal(48, loaded.IconSize);
        Assert.Equal("notepad", loaded.Targets[0].ProcessName);
        Assert.Equal("vi", loaded.Language);
        Assert.False(File.Exists(store.FilePath + ".tmp"));
    }

    [Fact]
    public void Language_UnknownValue_BecomesVietnamese()
    {
        var loaded = AppConfigNormalizer.Normalize(new AppConfig { Language = "fr" });
        Assert.Equal("vi", loaded.Language);
        Assert.Equal("en", AppConfigNormalizer.Normalize(new AppConfig { Language = "EN" }).Language);
    }

    [Fact]
    public void NinthTarget_IsRejected()
    {
        var config = AppConfig.CreateDefault();
        for (var i = 0; i < 8; i++)
        {
            Assert.True(AppConfigNormalizer.TryAddTarget(config, new TargetConfig { Title = i.ToString() }));
        }

        Assert.False(AppConfigNormalizer.TryAddTarget(config, new TargetConfig { Title = "9" }));
    }

    [Fact]
    public void Identity_MatchesOneToOne()
    {
        var targets = new List<TargetConfig>
        {
            new() { ProcessName = "notepad", ClassName = "Notepad", Title = "A" },
            new() { ProcessName = "notepad", ClassName = "Notepad", Title = "A" }
        };
        var windows = new List<WindowCandidate>
        {
            new(new WindowId(1), new WindowIdentity("Notepad", "notepad", "a")),
            new(new WindowId(2), new WindowIdentity("notepad", "Notepad", "A"))
        };
        var matched = WindowIdentityMatcher.Match(targets, windows);
        Assert.Equal(new WindowId(1), matched[0]);
        Assert.Equal(new WindowId(2), matched[1]);
        Assert.NotEqual(matched[0], matched[1]);
    }

    [Fact]
    public void UnknownMonitor_UsesDefaultNearCursor()
    {
        var target = new TargetConfig { MonitorDevice = @"\\.\DISPLAY9", IconX = 10, IconY = 10 };
        var work = new PixelRect(0, 0, 800, 600);
        var point = IconPlacement.Resolve(target, @"\\.\DISPLAY1", work, 48);
        var expected = IconPlacement.DefaultNearCursor(work, 48);
        Assert.Equal(expected, point);
    }

    [Fact]
    public void Clamp_KeepsIconInsideWorkArea()
    {
        var work = new PixelRect(0, 0, 200, 100);
        var clamped = IconPlacement.Clamp(new PixelPoint(1000, 1000), work, 48);
        Assert.Equal(152, clamped.X);
        Assert.Equal(52, clamped.Y);
    }

    [Fact]
    public void Catalog_DropsToolEmptyCloakedAndSelf()
    {
        Assert.False(WindowCatalogFilter.Include(Snap(tool: true), 1));
        Assert.False(WindowCatalogFilter.Include(Snap(title: ""), 1));
        Assert.False(WindowCatalogFilter.Include(Snap(cloaked: true), 1));
        Assert.False(WindowCatalogFilter.Include(Snap(pid: 9), 9));
        Assert.True(WindowCatalogFilter.Include(Snap(), 9));
    }

    [Fact]
    public void FullscreenClassifier_RequiresBorderlessCoverage()
    {
        var monitor = new PixelRect(0, 0, 1920, 1080);
        Assert.True(FullscreenClassifier.IsBorderlessFullscreen(monitor, monitor, false));
        Assert.False(FullscreenClassifier.IsBorderlessFullscreen(monitor, monitor, true));
        Assert.False(FullscreenClassifier.IsBorderlessFullscreen(new PixelRect(0, 0, 800, 600), monitor, false));
        Assert.True(FullscreenClassifier.IsShellDesktop("Progman"));
        Assert.True(FullscreenClassifier.IsShellDesktop("WorkerW"));
        Assert.False(FullscreenClassifier.IsShellDesktop("Notepad"));
        Assert.False(FullscreenClassifier.ShouldHideFloatIcon("Chrome_WidgetWin_1", true, false, false, monitor, monitor));
        Assert.False(FullscreenClassifier.ShouldHideFloatIcon("Progman", false, false, false, monitor, monitor));
        Assert.False(FullscreenClassifier.ShouldHideFloatIcon("Notepad", false, false, true, monitor, monitor));
        Assert.True(FullscreenClassifier.ShouldHideFloatIcon("ApplicationFrameWindow", false, false, false, monitor, monitor));
        Assert.False(FullscreenClassifier.ShouldHideFloatIcon("IHWindowClass", false, false, false, monitor, monitor));
        Assert.True(FullscreenClassifier.IsRemoteDesktopClient("TscShellContainerClass"));
    }

    private static WindowSnapshot Snap(bool tool = false, string title = "Title", bool cloaked = false, int pid = 2)
        => new(new WindowId(1), pid, "app", "Class", title, true, cloaked, tool, false);
}
