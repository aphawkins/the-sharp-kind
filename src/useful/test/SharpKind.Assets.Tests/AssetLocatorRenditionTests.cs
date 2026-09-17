// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;
using System.Text.Json;
using Xunit;

namespace SharpKind.Assets.Tests;

// A rendition keeps its assets in its own folder, so the folder is the answer and its manifest is the whole manifest.
public class AssetLocatorRenditionTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _assetsRoot;
    private bool _isDisposed;

    public AssetLocatorRenditionTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "SharpKind.Assets.Tests", Guid.NewGuid().ToString("N"));
        _assetsRoot = Path.Combine(_tempRoot, "Assets");
        Directory.CreateDirectory(_assetsRoot);
    }

    [Theory]
    [InlineData("Images", "logo.bmp")]
    [InlineData("Models", "ship.obj")]
    [InlineData("Fonts", "font1.bmp")]
    [InlineData("Fonts", "terminal.fon")]
    [InlineData("Fonts", "roboto.ttf")]
    [InlineData("Palette", "palette.json")]
    [InlineData("SFX", "beep.wav")]
    public void ResolvesEveryCategoryUnderTheFolderItWasPointedAt(string category, string file)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(file);

        AssetLocator locator = Locate("Anything");

        // Act
        string path = category switch
        {
            "Images" => locator.ImagePaths["Logo"],
            "Models" => locator.ModelPaths["Ship"],
            "Fonts" when file.EndsWith(".fon", StringComparison.Ordinal) => locator.FontFons["Small"].Path,
            "Fonts" when file.EndsWith(".ttf", StringComparison.Ordinal) => locator.FontTrueTypes["Small"].Path,
            "Fonts" => locator.FontBitmaps["Small"].Path,
            "Palette" => locator.PalettePath,
            _ => locator.SfxPaths["Beep"],
        };

        // Assert: no rendition segment anywhere in it.
        Assert.Equal(Path.Combine(_assetsRoot, category, file), path);
    }

    [Fact]
    public void CarriesTheNameItWasGivenWithoutPuttingItInAPath()
    {
        // The name is only a label for messages, not part of any path.
        AssetLocator locator = Locate("Psychedelic");

        Assert.Equal("Psychedelic", locator.Rendition);
        Assert.DoesNotContain("Psychedelic", locator.ImagePaths["Logo"], StringComparison.Ordinal);
    }

    [Fact]
    public void ReadsTheColourLimitsTheRenditionDeclared()
    {
        AssetLocator locator = Locate("Anything");

        Assert.Equal(16, locator.Colours.MaxColours);
        Assert.True(locator.Colours.PaletteNamesEveryColour);
        Assert.Equal(8, locator.Colours.ChannelBits);
    }

    [Fact]
    public void RefusesANameThatCouldNotBeAFolder()
    {
        // Used to find a rendition on disk, so a path separator in the name is refused rather than followed.
        using MemoryStream stream = ToStream(Manifest());

        Assert.Throws<SharpKindException>(() => AssetLocator.Create(stream, _tempRoot, "../escape"));
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        try
        {
            if (disposing && Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, true);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup; swallow exceptions to avoid masking test results.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup; swallow exceptions to avoid masking test results.
        }
    }

    private static MemoryStream ToStream(object manifest)
        => new(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(manifest)));

    private static object Manifest() => new
    {
        Colours = new { MaxColours = 16, PaletteNamesEveryColour = true, ChannelBits = 8 },
        Palette = "palette.json",
        Fonts = new
        {
            Bitmap = new Dictionary<string, object>
            {
                { "Small", new { File = "font1.bmp", CellWidth = 8, CellHeight = 8, Columns = 16 } },
            },
            Fon = new Dictionary<string, object>
            {
                { "Small", new { File = "terminal.fon", PixelHeight = 8 } },
            },
            TrueType = new Dictionary<string, object>
            {
                { "Small", new { File = "roboto.ttf", PointSize = 14 } },
            },
        },
        Images = new Dictionary<string, string> { { "Logo", "logo.bmp" } },
        Models = new Dictionary<string, string> { { "Ship", "ship.obj" } },
        Sfx = new Dictionary<string, string> { { "Beep", "beep.wav" } },
    };

    private AssetLocator Locate(string rendition)
    {
        using MemoryStream stream = ToStream(Manifest());
        return AssetLocator.Create(stream, _tempRoot, rendition);
    }
}
