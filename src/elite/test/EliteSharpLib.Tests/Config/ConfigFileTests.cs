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

        // The renditions' own sheets, so a commander who has chosen nothing
        // gets the text each rendition was drawn for.
        Assert.Equal(FontKind.Bitmap, config.Engine.Graphics.FontKind);
        Assert.Equal("8-bit", config.Engine.Rendition);
        Assert.Null(config.Engine.WindowScale);
        Assert.True(config.Engine.Sound.Music);
        Assert.True(config.Engine.Sound.Effects);
    }

    // A font kind the build does not know - a hand-edit, or a file from a
    // later build - goes back to the sheets rather than leaving the game
    // drawing with nothing.
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

    // A kind the build does know is left alone, whether or not the rendition
    // in use has such a font - what a rendition offers is settled when its
    // assets are loaded, not here.
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

    // The setting has to survive the file, not just the object: it is written
    // by the settings screen and read back at the next launch.
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
        // Arrange: a hand-edited/corrupt file where a bool field holds a
        // non-boolean string - Microsoft.Extensions.Configuration.Binder
        // wraps this as InvalidOperationException, not FormatException.
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
        // Arrange: exercises AddEliteConfig's actual repair, not just the
        // generic ConfigFile<T> plumbing. The unreadable fps must not cost
        // the user the settings either side of it.
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
        // Renditions were once named rather than enumerated, on the grounds
        // that the game cannot know what exists, and a name like this one was
        // kept for the loader to fail on by name. They are a closed set now -
        // 8-bit, 16-bit and Modern, defined by the engine - so a file naming
        // anything else names something that cannot exist, and it is repaired
        // like any other unusable value.
        //
        // It repairs to Elite's own fallback, the 8-bit tier, rather than to
        // the engine's 16-bit: a repair that named a rendition the game does
        // not ship would leave it unable to start.
        //
        // The rest of the file survives, which is the part that has not
        // changed: one bad value costs that value alone.
        EliteConfig config = ReadWritten(
            /*lang=json,strict*/ "{\"engine\": {\"rendition\": \"Psychedelic\"}, \"game\": {\"sunStyle\": \"Solid\"}}");

        Assert.Equal(RenditionNames.EightBit, config.Engine.Rendition);
        Assert.Equal(SunType.Solid, config.Game.SunStyle);
    }

    [Fact]
    public void ReadConfigKeepsTheModernRendition()
    {
        // The third of the three. No game draws it yet, but the engine names
        // it, so a file may hold it.
        EliteConfig config = ReadWritten(
            /*lang=json,strict*/ "{\"engine\": {\"rendition\": \"Modern\"}}");

        Assert.Equal(RenditionNames.Modern, config.Engine.Rendition);
    }

    // The limit of repairing in place: a value the binder cannot even parse
    // (a misspelt enum name, a string where a number belongs) fails the whole
    // bind, so there is nothing to repair and the defaults stand. The file
    // itself is kept as .bad, which is the only reason that is survivable.
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
        // The scale is independent of the rendition: a magnified 8-bit window
        // is the point of the setting, not a contradiction to repair away.
        EliteConfig config = ReadWritten(
            /*lang=json,strict*/ "{\"engine\": {\"windowScale\": 3, \"tier\": \"8Bit\"}}");

        Assert.Equal(3, config.Engine.WindowScale);
        Assert.Equal("8-bit", config.Engine.Rendition);
    }

    [Fact]
    public void ReadConfigStampsTheCurrentSchemaVersion()
    {
        // A file from before versioning has no version at all, and one from a
        // later build claims a version this one cannot honour; both are
        // brought back to what this build writes.
        Assert.Equal(ConfigSchema.CurrentVersion, ReadWritten(/*lang=json,strict*/ "{\"game\": {}}").Version);
        Assert.Equal(ConfigSchema.CurrentVersion, ReadWritten(/*lang=json,strict*/ "{\"version\": 99}").Version);
    }

    // A rendition is written under the name it calls itself. The game has no
    // spelling of its own to apply - it cannot have one for a rendition it
    // has never seen.
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

    // Files written before renditions existed say "tier", and spell it with a
    // digit. Both the old key and the old spelling have to survive, or every
    // config file written before this change quietly loses its choice.
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

    // The old key only wins where the new one was never written, so a file
    // holding both - which only a hand-edit produces - keeps the new one.
    // Modern is the new value here because it is a rendition neither the
    // default nor the legacy tier names, so only the new key can have put it
    // there.
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
