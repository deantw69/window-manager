using System.IO;
using WindowManager.Core;
using WindowManager.Persistence;
using Xunit;

namespace WindowManager.Tests;

public class PersistenceTests : IDisposable
{
    private readonly string _dir;

    public PersistenceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "WM_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { }
    }

    private string P(string name) => Path.Combine(_dir, name);

    [Fact]
    public void WriteAtomic_Then_Read_RoundTrips()
    {
        var path = P("data.json");
        var value = new LayoutSet { CapturedAt = "now", Windows = { new WindowLayout { Title = "T", X = 5 } } };

        JsonFileStore.WriteAtomic(path, value);
        var read = JsonFileStore.ReadOrDefault<LayoutSet>(path);

        Assert.NotNull(read);
        Assert.Single(read!.Windows);
        Assert.Equal(5, read.Windows[0].X);
        Assert.Equal("now", read.CapturedAt);
    }

    [Fact]
    public void WriteAtomic_LeavesNoTempFile()
    {
        var path = P("data.json");
        JsonFileStore.WriteAtomic(path, new LayoutSet());
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void WriteAtomic_OverwriteKeepsLatest()
    {
        var path = P("data.json");
        JsonFileStore.WriteAtomic(path, new LayoutSet { CapturedAt = "first" });
        JsonFileStore.WriteAtomic(path, new LayoutSet { CapturedAt = "second" });
        Assert.Equal("second", JsonFileStore.ReadOrDefault<LayoutSet>(path)!.CapturedAt);
    }

    [Fact]
    public void ReadOrDefault_MissingFile_ReturnsDefault()
        => Assert.Null(JsonFileStore.ReadOrDefault<LayoutSet>(P("nope.json")));

    [Fact]
    public void ReadOrDefault_Corrupt_BacksUpAndReturnsDefault()
    {
        var path = P("corrupt.json");
        File.WriteAllText(path, "{ this is not valid json ");

        var read = JsonFileStore.ReadOrDefault<LayoutSet>(path);

        Assert.Null(read);
        Assert.True(File.Exists(path + ".corrupt"), "毀損檔應被備份為 .corrupt");
        Assert.True(File.Exists(path), "原始毀損檔不應被刪除");
    }

    [Fact]
    public void LayoutStore_SaveLoad_RoundTrips()
    {
        var store = new LayoutStore(P("layouts.json"));
        store.Save(new LayoutSet { Windows = { new WindowLayout { Title = "A", State = ShowState.Maximized } } });

        var loaded = store.Load();
        Assert.Single(loaded.Windows);
        Assert.Equal(ShowState.Maximized, loaded.Windows[0].State);
    }

    [Fact]
    public void LayoutStore_WrongSchemaVersion_ReturnsEmpty()
    {
        var path = P("layouts.json");
        File.WriteAllText(path, "{\"SchemaVersion\":999,\"Windows\":[{\"Title\":\"X\"}]}");

        var loaded = new LayoutStore(path).Load();
        Assert.Empty(loaded.Windows);
    }

    [Fact]
    public void LayoutStore_Missing_ReturnsEmpty()
        => Assert.Empty(new LayoutStore(P("missing.json")).Load().Windows);

    [Fact]
    public void SettingsStore_SaveLoad_RoundTrips()
    {
        var store = new SettingsStore(P("settings.json"));
        store.Save(new AppSettings { AutoSaveEnabled = true, AutoSaveIntervalSeconds = 123 });

        var loaded = store.Load();
        Assert.True(loaded.AutoSaveEnabled);
        Assert.Equal(123, loaded.AutoSaveIntervalSeconds);
    }

    [Fact]
    public void SettingsStore_WrongVersion_ReturnsDefaults()
    {
        var path = P("settings.json");
        File.WriteAllText(path, "{\"SchemaVersion\":999,\"AutoSaveEnabled\":true}");

        var loaded = new SettingsStore(path).Load();
        Assert.False(loaded.AutoSaveEnabled); // 預設值
    }

    [Fact]
    public void SettingsStore_Missing_ReturnsDefaults()
    {
        var loaded = new SettingsStore(P("missing.json")).Load();
        Assert.True(loaded.HotkeysEnabled);
        Assert.False(loaded.AutoSaveEnabled);
    }
}
