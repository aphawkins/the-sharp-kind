// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Config;
using EliteSharpLib.Suns;
using SharpKind.Abstraction.Config;
using SharpKind.Abstraction.Renditions;
using SharpKind.Config;
using SharpKind.Graphics;

namespace EliteSharpLib.Tests.Config;

public class ConfigFileTests
{
    private const string ConfigFileName = "elite.sharp";

    [Fact]
    public void ReadConfigWithoutAFileReturnsDefaults()
    {
        // Arrange
        ConfigFile<EliteConfig> configFile = new(CreateTempDirectory(), ConfigFileName);

        // Act
        EliteConfig config = configFile.ReadConfig();

        // Assert
        Assert.Equal(60f, config.Engine.Graphics.Fps);
        Assert.False(config.Engine.Graphics.ShowFps);

        // Default is the renditions' own sheets, not a commander's choice.
        Assert.Equal(FontKind.Bitmap, config.Engine.Graphics.FontKind);
        Assert.Equal("8-bit", config.Engine.Rendition);
        Assert.Null(config.Engine.WindowScale);
        Assert.True(config.Engine.Sound.Music);
        Assert.True(config.Engine.Sound.Effects);
    }

    // An unknown font kind goes back to the sheets rather than drawing with nothing.
    [Fact]
    public void RepairReplacesAnUnknownFontKind()
    {
        // Arrange
        GraphicsConfigSettings graphics = new() { FontKind = (FontKind)42 };

        // Act
        bool repaired = graphics.Repair();

        // Assert
        Assert.True(repaired);
        Assert.Equal(FontKind.Bitmap, graphics.FontKind);
    }

    // A known kind is left alone; what a rendition offers is settled when its assets load, not here.
    [Fact]
    public void RepairKeepsAKnownFontKind()
    {
        // Arrange
        GraphicsConfigSettings graphics = new() { FontKind = FontKind.Fon };

        // Act
        graphics.Repair();

        // Assert
        Assert.Equal(FontKind.Fon, graphics.FontKind);
    }

    // Must survive the file, not just the object: written by the settings screen, read back next launch.
    [Fact]
    public void WriteConfigThenReadConfigKeepsTheFontKind()
    {
        // Arrange
        ConfigFile<EliteConfig> configFile = new(CreateTempDirectory(), ConfigFileName);
        EliteConfig written = new() { Engine = new() { Graphics = new() { FontKind = FontKind.Fon } } };

        // Act
        configFile.WriteConfig(written);
        EliteConfig read = configFile.ReadConfig();

        // Assert
        Assert.Equal(FontKind.Fon, read.Engine.Graphics.FontKind);
    }

    [Fact]
    public void WriteConfigThenReadConfigRoundTrips()
    {
        // Arrange
        ConfigFile<EliteConfig> configFile = new(CreateTempDirectory(), ConfigFileName);
        EliteConfig written = new()
        {
            Engine = new() { Sound = new() { Music = false, Effects = false } },
            Game = new() { InstantDock = true },
        };

        // Act
        configFile.WriteConfig(written);
        EliteConfig read = configFile.ReadConfig();

        // Assert
        Assert.False(read.Engine.Sound.Music);
        Assert.False(read.Engine.Sound.Effects);
        Assert.True(read.Game.InstantDock);
    }

    [Fact]
    public void ReadConfigWithAMistypedValueReturnsDefaultsInsteadOfThrowing()
    {
        // A bool field holding a non-boolean string: the binder wraps this as InvalidOperationException, not FormatException.
        string directory = CreateTempDirectory();
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, ConfigFileName), /*lang=json,strict*/ "{\"game\": {\"instantDock\": \"hello!\"}}");
        ConfigFile<EliteConfig> configFile = new(directory, ConfigFileName);

        // Act
        EliteConfig config = configFile.ReadConfig();

        // Assert
        Assert.False(config.Game.InstantDock);
    }

    [Fact]
    public void ReadConfigWithInvalidFpsRepairsOnlyTheFps()
    {
        // Exercises AddEliteConfig's actual repair; the unreadable fps must not cost the settings either side of it.
        EliteConfig config = ReadWritten(
            /*lang=json,strict*/ "{\"engine\": {\"graphics\": {\"fps\": 0}}, \"game\": {\"instantDock\": true}}");

        // Assert
        Assert.Equal(60f, config.Engine.Graphics.Fps);
        Assert.True(config.Game.InstantDock);
    }

    [Fact]
    public void ReadConfigHonoursShowFps()
    {
        EliteConfig config = ReadWritten(
            /*lang=json,strict*/ "{\"engine\": {\"graphics\": {\"showFps\": true}}}");

        Assert.True(config.Engine.Graphics.ShowFps);
    }

    [Fact]
    public void ReadConfigRepairsARenditionNameThatIsNotOneOfTheThree()
    {
        // Renditions are a closed set, so an unrecognised name is repaired - to Elite's own
        // fallback, since the engine's 16-bit is one it does ship but need not. The rest of the
        // file survives: one bad value costs that value alone.
        EliteConfig config = ReadWritten(
            /*lang=json,strict*/ "{\"engine\": {\"rendition\": \"Psychedelic\"}, \"game\": {\"sunStyle\": \"Solid\"}}");

        Assert.Equal(RenditionNames.EightBit, config.Engine.Rendition);
        Assert.Equal(SunType.Solid, config.Game.SunStyle);
    }

    [Fact]
    public void ReadConfigKeepsTheModernRendition()
    {
        // No game draws it yet, but the engine names it, so a file may hold it.
        EliteConfig config = ReadWritten(
            /*lang=json,strict*/ "{\"engine\": {\"rendition\": \"Modern\"}}");

        Assert.Equal(RenditionNames.Modern, config.Engine.Rendition);
    }

    // An unparseable value fails the whole bind; defaults stand and the file is kept as .bad.
    [Fact]
    public void ReadConfigWithAnUnparseableValueFallsBackToDefaultsAndKeepsTheFile()
    {
        string directory = CreateTempDirectory();
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, ConfigFileName),
            /*lang=json,strict*/ "{\"engine\": {\"windowScale\": \"lots\"}, \"game\": {\"instantDock\": true}}");
        ConfigFile<EliteConfig> configFile = new(directory, ConfigFileName, EliteServiceCollectionExtensions.RepairConfig);

        EliteConfig config = configFile.ReadConfig();

        Assert.Equal("8-bit", config.Engine.Rendition);
        Assert.False(config.Game.InstantDock);
        Assert.True(File.Exists(Path.Combine(directory, ConfigFileName + ".bad")));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    [InlineData(5)]
    public void ReadConfigWithAnUnusableWindowScaleRepairsThatSettingAlone(int scale)
    {
        EliteConfig config = ReadWritten(
            $"{{\"engine\": {{\"windowScale\": {scale}, \"tier\": \"8Bit\"}}}}");

        Assert.Null(config.Engine.WindowScale);
        Assert.Equal("8-bit", config.Engine.Rendition);
    }

    [Fact]
    public void ReadConfigKeepsAWindowScaleItCanHonour()
    {
        // Scale is independent of rendition: a magnified 8-bit window is the point, not a contradiction.
        EliteConfig config = ReadWritten(
            /*lang=json,strict*/ "{\"engine\": {\"windowScale\": 3, \"tier\": \"8Bit\"}}");

        Assert.Equal(3, config.Engine.WindowScale);
        Assert.Equal("8-bit", config.Engine.Rendition);
    }

    [Fact]
    public void ReadConfigStampsTheCurrentSchemaVersion()
    {
        // Missing or unrecognised version both get stamped with what this build writes.
        Assert.Equal(ConfigSchema.CurrentVersion, ReadWritten(/*lang=json,strict*/ "{\"game\": {}}").Version);
        Assert.Equal(ConfigSchema.CurrentVersion, ReadWritten(/*lang=json,strict*/ "{\"version\": 99}").Version);
    }

    // A rendition is written under the name it calls itself; the game has no spelling of its own to apply.
    [Theory]
    [InlineData("8-bit")]
    [InlineData("Psychedelic")]
    public void WriteConfigWritesTheRenditionName(string rendition)
    {
        string directory = CreateTempDirectory();
        ConfigFile<EliteConfig> configFile = new(directory, ConfigFileName);

        configFile.WriteConfig(new() { Engine = new() { Rendition = rendition } });

        string json = File.ReadAllText(Path.Combine(directory, ConfigFileName));
        Assert.Contains($"\"rendition\": \"{rendition}\"", json, StringComparison.Ordinal);
    }

    // Old files use the "tier" key with digit spelling; both must still be read or old configs lose their choice.
    [Theory]
    [InlineData("8Bit", "8-bit")]
    [InlineData("16Bit", "16-bit")]
    [InlineData("EightBit", "8-bit")]
    public void RepairUpgradesTheOldTierSetting(string written, string expected)
    {
        EngineConfigSettings engine = new() { Tier = written };

        Assert.True(engine.Repair());
        Assert.Equal(expected, engine.Rendition);
        Assert.Null(engine.Tier);
    }

    // The old key only wins where the new one was never written. Modern here because neither the
    // default nor the legacy tier names it, so only the new key can have put it there.
    [Fact]
    public void RepairKeepsTheRenditionWhenBothAreSet()
    {
        EngineConfigSettings engine = new() { Rendition = RenditionNames.Modern, Tier = "8Bit" };

        Assert.True(engine.Repair());
        Assert.Equal(RenditionNames.Modern, engine.Rendition);
    }

    private static EliteConfig ReadWritten(string json)
    {
        string directory = CreateTempDirectory();
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, ConfigFileName), json);
        ConfigFile<EliteConfig> configFile = new(directory, ConfigFileName, EliteServiceCollectionExtensions.RepairConfig);

        return configFile.ReadConfig();
    }

    private static string CreateTempDirectory()
        => Path.Combine(Path.GetTempPath(), "ConfigFileTests_" + Guid.NewGuid().ToString("N"));
}
