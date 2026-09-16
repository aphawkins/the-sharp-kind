// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Fakes;
using SharpKind.Fakes.Assets;
using SharpKind.Fakes.Input;
using Xunit;

namespace BubbleBobbleSharpLib.Tests;

public sealed class BubbleBobbleMainTests
{
    [Fact]
    public void NewGameIsRunning()
    {
        BubbleBobbleMain game = new(new FakeAbstraction(), new FakeAssetLocator());

        Assert.True(game.IsRunning);
    }

    [Fact]
    public void EscapeStopsTheGame()
    {
        FakeAbstraction abstraction = new();
        BubbleBobbleMain game = new(abstraction, new FakeAssetLocator());
        ((FakeKeyboard)abstraction.Keyboard).KeyDown(ConsoleKey.Escape, ConsoleModifiers.None);

        game.Update();

        Assert.False(game.IsRunning);
    }
}
