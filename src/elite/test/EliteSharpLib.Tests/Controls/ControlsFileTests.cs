// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Text.Json;
using EliteSharpLib.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharpKind.Abstraction.Config;
using SharpKind.Abstraction.Controls;
using SharpKind.Fakes.Input;

namespace EliteSharpLib.Tests.Controls;

// The file has to appear on disk, filled in, the first time the game runs -
// there is nothing to edit otherwise, and an empty one is every control
// dead. ConfigFile does not repair a file that was never there, which is
// why the registration writes the defaults rather than binding over them.
public sealed class ControlsFileTests : IDisposable
{
    private const string FileName = "elite.controls.sharp";

    private readonly string _directory
        = Path.Combine(Path.GetTempPath(), "EliteControlsFileTests_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void AFirstRunWritesTheBindingsOut()
    {
        ControlBindings bindings = Resolve();

        Assert.True(File.Exists(Path.Combine(_directory, FileName)));
        Assert.NotEmpty(bindings.Keyboard);
        Assert.Equal(3, bindings.Controllers.Count);
    }

    // What lands on disk has to be something a commander can read and
    // change, so the action names are in it and so are the keys.
    [Fact]
    public void TheWrittenFileNamesItsActionsAndKeys()
    {
        _ = Resolve();

        using JsonDocument written = JsonDocument.Parse(File.ReadAllText(Path.Combine(_directory, FileName)));
        JsonElement keyboard = written.RootElement.GetProperty("keyboard");

        Assert.Equal("A", FirstKeyFor(keyboard, nameof(EliteAction.FireLaser)));
        Assert.Equal("F1", FirstKeyFor(keyboard, nameof(EliteAction.FrontView)));
    }

    [Fact]
    public void TheWrittenFileNamesEveryController()
    {
        _ = Resolve();

        string written = File.ReadAllText(Path.Combine(_directory, FileName));

        Assert.Contains("SideWinder", written, StringComparison.Ordinal);
        Assert.Contains("STK-7024X", written, StringComparison.Ordinal);
        Assert.Contains(ControlBindings.AnyDevice, written, StringComparison.Ordinal);
    }

    // One key is a bare value and several are an array: most actions have
    // one, and a one-item array is noise in a file meant to be hand-edited.
    [Fact]
    public void OneKeyIsWrittenBareAndSeveralAsAnArray()
    {
        _ = Resolve();

        using JsonDocument written = JsonDocument.Parse(File.ReadAllText(Path.Combine(_directory, FileName)));
        JsonElement keyboard = written.RootElement.GetProperty("keyboard");

        Assert.Equal(JsonValueKind.String, ValueFor(keyboard, nameof(EliteAction.FireLaser)).ValueKind);
        Assert.Equal(JsonValueKind.Array, ValueFor(keyboard, nameof(EliteAction.PitchUp)).ValueKind);
    }

    // The form the file is written in has to be the form it can be read in.
    // The configuration binder drops a bare value silently unless the type
    // can convert from one, so this is what proves the round trip.
    [Fact]
    public void TheBareFormReadsBackAgain()
    {
        ControlBindings first = Resolve();
        Assert.True(Bound(first, EliteAction.FireLaser, ConsoleKey.A));

        ControlBindings reread = Resolve();

        Assert.True(Bound(reread, EliteAction.FireLaser, ConsoleKey.A));
        Assert.True(Bound(reread, EliteAction.PitchUp, ConsoleKey.UpArrow));
    }

    // A commander may write either form by hand, so both are read.
    [Fact]
    public void EitherFormMayBeWrittenByHand()
    {
        _ = Resolve();
        Rewrite(text => text.Replace("\"FireLaser\": \"A\"", "\"FireLaser\": [ \"Z\" ]", StringComparison.Ordinal));

        Assert.True(Bound(Resolve(), EliteAction.FireLaser, ConsoleKey.Z));
    }

    [Fact]
    public void TheFileCarriesItsSchemaVersion()
    {
        ControlBindings bindings = Resolve();

        using JsonDocument written = JsonDocument.Parse(File.ReadAllText(Path.Combine(_directory, FileName)));

        Assert.Equal(bindings.Version, written.RootElement.GetProperty("version").GetInt32());
        Assert.Equal(ConfigSchema.CurrentVersion, bindings.Version);
    }

    // A version from a later build is stamped back rather than obeyed:
    // there is nothing to migrate to, and the original is kept alongside.
    [Fact]
    public void AVersionFromTheFutureIsStampedBack()
    {
        _ = Resolve();
        Rewrite(text => text.Replace("\"version\": 1", "\"version\": 99", StringComparison.Ordinal));

        Assert.Equal(ConfigSchema.CurrentVersion, Resolve().Version);
    }

    // A hand-edit has to survive the next run, or the file would be
    // pointless: what goes back is what was just read.
    [Fact]
    public void AHandEditSurvivesTheNextRun()
    {
        _ = Resolve();
        Rewrite(text => text.Replace("\"A\"", "\"Z\"", StringComparison.Ordinal));

        ControlBindings reread = Resolve();

        Assert.True(Bound(reread, EliteAction.FireLaser, ConsoleKey.Z));
    }

    // A commander typing by hand will not match the file's casing and
    // should not have to.
    [Fact]
    public void AHandEditNeedNotMatchTheCasing()
    {
        _ = Resolve();
        Rewrite(text => text
            .Replace(nameof(EliteAction.FireLaser), "firelaser", StringComparison.Ordinal)
            .Replace("\"A\"", "\"z\"", StringComparison.Ordinal));

        ControlBindings reread = Resolve();

        Assert.True(Bound(reread, EliteAction.FireLaser, ConsoleKey.Z));
    }

    // A binding the game cannot honour costs that line, and the original is
    // left alongside as .bad rather than quietly discarded.
    [Fact]
    public void ABadBindingIsDroppedAndTheOriginalKept()
    {
        _ = Resolve();
        Rewrite(text => text.Replace(nameof(EliteAction.FireLaser), "PolishTheHull", StringComparison.Ordinal));

        ControlBindings reread = Resolve();

        Assert.DoesNotContain("PolishTheHull", reread.Keyboard.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.True(File.Exists(Path.Combine(_directory, FileName) + ".bad"));
    }

    // Removing a binding unbinds that control: the file is the answer, not
    // a patch over something in the code.
    [Fact]
    public void RemovingABindingUnbindsTheControl()
    {
        _ = Resolve();
        Rewrite(text => text.Replace(nameof(EliteAction.Ecm), "PolishTheHull", StringComparison.Ordinal));

        ControlBindings reread = Resolve();

        Assert.False(Bound(reread, EliteAction.Ecm, ConsoleKey.E));
    }

    // Dictionary keys keep the casing they were written with, so a lookup
    // by name has to ignore case the way the game does. The value is a bare
    // string for one key and an array for several.
    private static string? FirstKeyFor(in JsonElement keyboard, string action)
    {
        JsonElement value = ValueFor(keyboard, action);

        return value.ValueKind == JsonValueKind.Array ? value[0].GetString() : value.GetString();
    }

    private static JsonElement ValueFor(in JsonElement keyboard, string action)
    {
        foreach (JsonProperty property in keyboard.EnumerateObject())
        {
            if (string.Equals(property.Name, action, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        throw new InvalidOperationException($"No binding written for '{action}'.");
    }

    // Asked through a EliteControlMap rather than off the dictionary, because
    // that is how the game asks and it is what actually has to work.
    private static bool Bound(ControlBindings bindings, EliteAction action, ConsoleKey key)
    {
        FakeKeyboard keyboard = new();
        EliteControlMap controls = new(bindings, keyboard, new FakeGamepad());
        keyboard.KeyDown(key, default);

        return controls.IsHeld(action);
    }

    private void Rewrite(Func<string, string> edit)
    {
        string path = Path.Combine(_directory, FileName);
        File.WriteAllText(path, edit(File.ReadAllText(path)));
    }

    private ControlBindings Resolve()
    {
        ServiceCollection services = new();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddEliteControls(_directory);

        using ServiceProvider provider = services.BuildServiceProvider();

        return provider.GetRequiredService<ControlBindings>();
    }
}
