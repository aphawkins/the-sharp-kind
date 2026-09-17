// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;
using System.Text.Json;
using Xunit;

namespace SharpKind.Assets.Tests;

public class AssetLocatorTests
{
    [Fact]
    public void CreateFromStreamBuildsPaths()
    {
        // Arrange
        string tempRoot = Path.Combine(Path.GetTempPath(), "SharpKind.Assets.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        string assetsRoot = Path.Combine(tempRoot, "Assets");
        Directory.CreateDirectory(assetsRoot);
        Directory.CreateDirectory(Path.Combine(assetsRoot, "Palette"));
        Directory.CreateDirectory(Path.Combine(assetsRoot, "Fonts"));
        Directory.CreateDirectory(Path.Combine(assetsRoot, "Images"));
        Directory.CreateDirectory(Path.Combine(assetsRoot, "Music"));
        Directory.CreateDirectory(Path.Combine(assetsRoot, "SFX"));
        Directory.CreateDirectory(Path.Combine(assetsRoot, "Models"));
        Directory.CreateDirectory(Path.Combine(assetsRoot, "SoundFonts"));

        string manifestPath = Path.Combine(assetsRoot, "AssetManifest.json");

        object manifestObject = new
        {
            Palette = "palette.png",
            Fonts = new
            {
                Bitmap = new Dictionary<string, object>
                {
                    { "Arial", new { File = "arial.png", CellWidth = 8, CellHeight = 8, Columns = 16 } },
                    { "Vera", new { File = "vera.png", CellWidth = 8, CellHeight = 8, Columns = 16 } },
                },
                Fon = new Dictionary<string, object>
                {
                    { "Terminal", new { File = "terminal.fon", PixelHeight = 10 } },
                },
                TrueType = new Dictionary<string, object>
                {
                    { "Roboto", new { File = "roboto.ttf", PointSize = 14 } },
                },
            },
            Images = new Dictionary<string, string>
            {
                { "Logo", "logo.png" },
            },
            Music = new Dictionary<string, string>
            {
                { "Theme", "theme.mp3" },
            },
            Sfx = new Dictionary<string, string>
            {
                { "Click", "click.wav" },
            },
            Models = new Dictionary<string, string>
            {
                { "Ship", "ship.model" },
            },
            SoundFonts = new Dictionary<string, string>
            {
                { "Fontella", "fontella.sf2" },
            },
        };

        string manifestJson = JsonSerializer.Serialize(manifestObject);
        File.WriteAllText(manifestPath, manifestJson);

        try
        {
            using FileStream stream = File.OpenRead(manifestPath);

            // Act
            AssetLocator locator = AssetLocator.Create(stream, tempRoot);

            // Assert - Palette
            string expectedPalette = Path.Combine(assetsRoot, "Palette", "palette.png");
            Assert.Equal(expectedPalette, locator.PalettePath);

            // Assert - the sheets
            IDictionary<string, BitmapFontAsset> fontBitmaps = locator.FontBitmaps;
            Assert.Equal(2, fontBitmaps.Count);
            Assert.Equal(Path.Combine(assetsRoot, "Fonts", "arial.png"), fontBitmaps["Arial"].Path);
            Assert.Equal(Path.Combine(assetsRoot, "Fonts", "vera.png"), fontBitmaps["Vera"].Path);

            // Assert - the .fon strikes
            IDictionary<string, FonFontAsset> fontFons = locator.FontFons;
            Assert.Single(fontFons);
            Assert.Equal(Path.Combine(assetsRoot, "Fonts", "terminal.fon"), fontFons["Terminal"].Path);
            Assert.Equal(10, fontFons["Terminal"].PixelHeight);

            // Assert - the TrueType faces
            IDictionary<string, TrueTypeFontAsset> fontTrueTypes = locator.FontTrueTypes;
            Assert.Single(fontTrueTypes);
            Assert.Equal(Path.Combine(assetsRoot, "Fonts", "roboto.ttf"), fontTrueTypes["Roboto"].Path);
            Assert.Equal(14, fontTrueTypes["Roboto"].PointSize);

            // Assert - Images
            IDictionary<string, string> imagePaths = locator.ImagePaths;
            Assert.Single(imagePaths);
            Assert.Equal(Path.Combine(assetsRoot, "Images", "logo.png"), imagePaths["Logo"]);

            // Assert - Music
            IDictionary<string, string> musicPaths = locator.MusicPaths;
            Assert.Single(musicPaths);
            Assert.Equal(Path.Combine(assetsRoot, "Music", "theme.mp3"), musicPaths["Theme"]);

            // Assert - Sfx
            IDictionary<string, string> sfxPaths = locator.SfxPaths;
            Assert.Single(sfxPaths);
            Assert.Equal(Path.Combine(assetsRoot, "SFX", "click.wav"), sfxPaths["Click"]);

            // Assert - Models
            IDictionary<string, string> modelPaths = locator.ModelPaths;
            Assert.Single(modelPaths);
            Assert.Equal(Path.Combine(assetsRoot, "Models", "ship.model"), modelPaths["Ship"]);

            // Assert - SoundFonts
            IDictionary<string, string> soundFontPaths = locator.SoundFontPaths;
            Assert.Single(soundFontPaths);
            Assert.Equal(Path.Combine(assetsRoot, "SoundFonts", "fontella.sf2"), soundFontPaths["Fontella"]);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, true);
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
    }

    [Fact]
    public void CreateDefaultThrowsWhenManifestMissing()
    {
        // Arrange
        string baseDir = Path.GetDirectoryName(AppContext.BaseDirectory) ?? string.Empty;
        string assetsRoot = Path.Combine(baseDir, "Assets");
        string manifestPath = Path.Combine(assetsRoot, "AssetManifest.json");

        if (File.Exists(manifestPath))
        {
            File.Delete(manifestPath);
        }

        // Ensure Assets dir exists but manifest missing
        Directory.CreateDirectory(assetsRoot);

        try
        {
            // Act & Assert
            Assert.Throws<SharpKindException>(AssetLocator.Create);
        }
        finally
        {
            try
            {
                if (Directory.Exists(assetsRoot))
                {
                    Directory.Delete(assetsRoot, true);
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
    }

    [Fact]
    public void CreateFromStreamNullStreamThrowsArgumentNullException()
    {
        // Arrange
        Stream? nullStream = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => AssetLocator.Create(nullStream!, "ignored"));
    }

    [Fact]
    public void CreateFromStreamInvalidJsonThrowsSharpKindException()
    {
        // Arrange
        string tempRoot = Path.Combine(Path.GetTempPath(), "SharpKind.Assets.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        string assetsRoot = Path.Combine(tempRoot, "Assets");
        Directory.CreateDirectory(assetsRoot);

        try
        {
            using MemoryStream ms = new();
            using StreamWriter sw = new(ms, Encoding.UTF8, 1024, true);
            sw.Write("{ invalid json");
            sw.Flush();
            ms.Position = 0;

            // Act & Assert
            Assert.Throws<SharpKindException>(() => AssetLocator.Create(ms, tempRoot));
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, true);
                }
            }
            catch (IOException)
            {
                // ignore cleanup errors
            }
            catch (UnauthorizedAccessException)
            {
                // ignore cleanup errors
            }
        }
    }

    [Fact]
    public void CreateFromStreamNullJsonThrowsSharpKindException()
    {
        // Arrange
        string tempRoot = Path.Combine(Path.GetTempPath(), "SharpKind.Assets.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        string assetsRoot = Path.Combine(tempRoot, "Assets");
        Directory.CreateDirectory(assetsRoot);

        try
        {
            using MemoryStream ms = new();
            using StreamWriter sw = new(ms, Encoding.UTF8, 1024, true);
            sw.Write("null");
            sw.Flush();
            ms.Position = 0;

            // Act & Assert
            Assert.Throws<SharpKindException>(() => AssetLocator.Create(ms, tempRoot));
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, true);
                }
            }
            catch (IOException)
            {
                // ignore cleanup errors
            }
            catch (UnauthorizedAccessException)
            {
                // ignore cleanup errors
            }
        }
    }

    // A misspelled section reads as declaring nothing otherwise, failing much later with an asset not found.
    [Fact]
    public void RefusesAManifestSectionItDoesNotKnow()
    {
        // Arrange - Font, where the section is called Fonts.
        object manifestObject = new
        {
            Font = new
            {
                Bitmap = new Dictionary<string, object>
                {
                    { "Small", new { File = "font1.bmp", CellWidth = 8, CellHeight = 8, Columns = 16 } },
                },
            },
        };

        using MemoryStream stream = new(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(manifestObject)));

        // Act
        SharpKindException exception = Assert.Throws<SharpKindException>(
            () => AssetLocator.Create(stream, Path.GetTempPath()));

        // Assert
        Assert.Contains("Failed to read asset manifest", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateDefaultReadsManifestFileReturnsLocator()
    {
        // Arrange
        string baseDir = Path.GetDirectoryName(AppContext.BaseDirectory) ?? string.Empty;
        string assetsRoot = Path.Combine(baseDir, "Assets");
        Directory.CreateDirectory(assetsRoot);
        Directory.CreateDirectory(Path.Combine(assetsRoot, "Palette"));
        Directory.CreateDirectory(Path.Combine(assetsRoot, "Fonts"));
        Directory.CreateDirectory(Path.Combine(assetsRoot, "Images"));
        Directory.CreateDirectory(Path.Combine(assetsRoot, "Music"));
        Directory.CreateDirectory(Path.Combine(assetsRoot, "SFX"));
        Directory.CreateDirectory(Path.Combine(assetsRoot, "Models"));
        Directory.CreateDirectory(Path.Combine(assetsRoot, "SoundFonts"));

        string manifestPath = Path.Combine(assetsRoot, "AssetManifest.json");

        object manifestObject = new
        {
            Palette = "palette.png",
            Fonts = new
            {
                Bitmap = new Dictionary<string, object>
                {
                    { "Arial", new { File = "arial.png", CellWidth = 8, CellHeight = 8, Columns = 16 } },
                },
                TrueType = new Dictionary<string, object>
                {
                    { "Roboto", new { File = "roboto.ttf", PointSize = 14 } },
                },
            },
            Images = new Dictionary<string, string>
            {
                { "Logo", "logo.png" },
            },
            Music = new Dictionary<string, string>
            {
                { "Theme", "theme.mp3" },
            },
            Sfx = new Dictionary<string, string>
            {
                { "Click", "click.wav" },
            },
            Models = new Dictionary<string, string>
            {
                { "Ship", "ship.model" },
            },
            SoundFonts = new Dictionary<string, string>
            {
                { "Fontella", "fontella.sf2" },
            },
        };

        string manifestJson = JsonSerializer.Serialize(manifestObject);
        File.WriteAllText(manifestPath, manifestJson);

        try
        {
            // Act
            AssetLocator locator = AssetLocator.Create();

            // Assert - simple sanity checks
            Assert.Equal(Path.Combine(assetsRoot, "Palette", "palette.png"), locator.PalettePath);
            Assert.Single(locator.FontBitmaps);
            Assert.Single(locator.FontTrueTypes);
            Assert.Single(locator.ImagePaths);
            Assert.Single(locator.MusicPaths);
            Assert.Single(locator.SfxPaths);
            Assert.Single(locator.ModelPaths);
            Assert.Single(locator.SoundFontPaths);
        }
        finally
        {
            try
            {
                if (File.Exists(manifestPath))
                {
                    File.Delete(manifestPath);
                }

                if (Directory.Exists(assetsRoot))
                {
                    Directory.Delete(assetsRoot, true);
                }
            }
            catch (IOException)
            {
                // ignore cleanup errors
            }
            catch (UnauthorizedAccessException)
            {
                // ignore cleanup errors
            }
        }
    }
}
