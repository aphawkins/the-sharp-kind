// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Renditions;
using Microsoft.Extensions.Logging.Abstractions;
using SharpKind.Abstraction.Renditions;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Renditions;

[Trait("Level", "Integration")]
public sealed class RenditionLoaderTests : IDisposable
{
    private const string RenditionFolder = "BubbleBobbleSharp.Renditions.EightBit";
    private const string RenditionAssembly = RenditionFolder + ".dll";

    private readonly string _baseDirectory;
    private bool _isDisposed;

    public RenditionLoaderTests()
    {
        _baseDirectory = Path.Combine(
            Path.GetTempPath(),
            "BubbleBobbleSharpLib.Tests.Renditions",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_baseDirectory);
    }

    [Fact]
    public void FindsTheRenditionTheConfigNames()
    {
        // Copied in off disk: neither the game nor this test hands the loader anything.
        GivenTheShippedRendition();

        InstalledRenditions found = RenditionLoader.LoadFrom(_baseDirectory, RenditionNames.EightBit, NullLogger.Instance);

        Assert.Equal(Rendition.EightBit, found.Chosen.Rendition);
        Assert.Equal([RenditionNames.EightBit], found.Names);
    }

    [Fact]
    public void TheRenditionSaysWhatSizeTheGameDrawsAt()
    {
        GivenTheShippedRendition();

        InstalledRenditions found = RenditionLoader.LoadFrom(_baseDirectory, RenditionNames.EightBit, NullLogger.Instance);

        // 40x25 character cells of 8x8 pixels, which is the C64's screen.
        Assert.Equal(320, found.Chosen.ScreenWidth);
        Assert.Equal(200, found.Chosen.ScreenHeight);
    }

    [Fact]
    public void ThrowsWhenNothingGoesByThatName()
    {
        // Fatal at startup, naming what it could not find.
        GivenTheShippedRendition();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => RenditionLoader.LoadFrom(_baseDirectory, "amiga", NullLogger.Instance));

        Assert.Contains("amiga", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThrowsWhenThePluginFolderHoldsNothing()
    {
        Directory.CreateDirectory(Path.Combine(_baseDirectory, RenditionLoader.FolderName));

        Assert.Throws<InvalidOperationException>(
            () => RenditionLoader.LoadFrom(_baseDirectory, RenditionNames.EightBit, NullLogger.Instance));
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        if (Directory.Exists(_baseDirectory))
        {
            Directory.Delete(_baseDirectory, recursive: true);
        }

        _isDisposed = true;
    }

    // A folder of its own, which is how the game ships it.
    private void GivenTheShippedRendition()
    {
        string folder = Path.Combine(_baseDirectory, RenditionLoader.FolderName, RenditionFolder);
        Directory.CreateDirectory(folder);
        File.Copy(
            Path.Combine(AppContext.BaseDirectory, RenditionLoader.FolderName, RenditionFolder, RenditionAssembly),
            Path.Combine(folder, RenditionAssembly));
    }
}
