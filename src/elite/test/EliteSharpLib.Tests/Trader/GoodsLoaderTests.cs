// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Trading;
using EliteSharpLib.Trader;
using Microsoft.Extensions.Logging.Abstractions;

namespace EliteSharpLib.Tests.Trader;

[Trait("Level", "Integration")]
public sealed class GoodsLoaderTests : IDisposable
{
    private const string TestPluginAssembly = "EliteSharp.Goods.TestPlugin.dll";
    private const string ClassicAssembly = "EliteSharp.Goods.Classic.dll";
    private const string GoodsDataFile = "goods.json";

    private readonly string _baseDirectory;
    private bool _isDisposed;

    public GoodsLoaderTests()
    {
        _baseDirectory = Path.Combine(Path.GetTempPath(), "EliteSharpLib.Tests.Goods", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_baseDirectory);
    }

    [Fact]
    public void ThrowsAndNamesTheFolderWhenThereIsNoGoodsFolder()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => GoodsLoader.LoadFrom(_baseDirectory, NullLogger.Instance));

        Assert.Contains(GoodsLoader.FolderName, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThrowsWhenTheGoodsFolderIsEmpty()
    {
        _ = Directory.CreateDirectory(GoodsFolder());

        Assert.Throws<InvalidOperationException>(() => GoodsLoader.LoadFrom(_baseDirectory, NullLogger.Instance));
    }

    [Fact]
    public void FindsASetInAnAssemblyItWasNeverBuiltAgainst()
    {
        GivenPlugin(TestPluginAssembly);

        IGoodsSet set = GoodsLoader.LoadFrom(_baseDirectory, NullLogger.Instance);

        Assert.Equal("Barter", set.Name);
    }

    [Fact]
    public void HandsBackASetThatWorks()
    {
        GivenPlugin(TestPluginAssembly);

        IGoodsSet set = GoodsLoader.LoadFrom(_baseDirectory, NullLogger.Instance);

        Assert.Contains(set.Goods, good => good.Id == "Grain");
    }

    [Fact]
    public void ThrowsAndNamesBothWhenTwoSetsAreInstalled()
    {
        GivenPlugin(TestPluginAssembly);
        GivenPlugin(ClassicAssembly);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => GoodsLoader.LoadFrom(_baseDirectory, NullLogger.Instance));

        Assert.Contains("Barter", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Classic", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SkipsAFileThatIsNotAnAssemblyAndCarriesOn()
    {
        GivenPlugin(TestPluginAssembly);
        File.WriteAllText(Path.Combine(GoodsFolder(), "rubbish.dll"), "not an assembly");

        IGoodsSet set = GoodsLoader.LoadFrom(_baseDirectory, NullLogger.Instance);

        Assert.Equal("Barter", set.Name);
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            try
            {
                Directory.Delete(_baseDirectory, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // The loaded assembly keeps its file open; a leftover temp folder must not fail a test.
            }

            _isDisposed = true;
        }
    }

    private string GoodsFolder() => Path.Combine(_baseDirectory, GoodsLoader.FolderName);

    // The classic set reads its table from beside its own assembly, so a copy
    // of it that leaves the table behind is one that cannot be constructed.
    private void GivenPlugin(string assembly)
    {
        string folder = Directory.CreateDirectory(GoodsFolder()).FullName;
        File.Copy(Path.Combine(AppContext.BaseDirectory, assembly), Path.Combine(folder, assembly));

        string data = Path.Combine(AppContext.BaseDirectory, GoodsDataFile);
        if (File.Exists(data))
        {
            File.Copy(data, Path.Combine(folder, GoodsDataFile), overwrite: true);
        }
    }
}
