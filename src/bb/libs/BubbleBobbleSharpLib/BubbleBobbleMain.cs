// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Runtime.CompilerServices;
using SharpKind.Abstraction;
using SharpKind.Assets;
using SharpKind.Audio;
using SharpKind.Graphics;
using SharpKind.Input;

[assembly: CLSCompliant(false)]
[assembly: InternalsVisibleTo("BubbleBobbleSharpLib.Tests")]

namespace BubbleBobbleSharpLib;

/// <summary>
/// The game, as the host and the composition root see it. It opens, clears
/// the screen and closes; see docs/bb-port-plan.md for what comes next.
/// </summary>
public sealed class BubbleBobbleMain : IGame, IGameApp
{
    // The C64 runs off the PAL raster interrupt, so one tick is one frame. Every counter
    // translated out of the reference is measured in these.
    internal const int TickRate = 50;

    private readonly IAbstraction _abstraction;

    public BubbleBobbleMain(IAbstraction abstraction, IAssetLocator assetLocator)
        : this(abstraction, assetLocator, new())
    {
    }

    public BubbleBobbleMain(IAbstraction abstraction, IAssetLocator assetLocator, AudioOptions audioOptions)
    {
        ArgumentNullException.ThrowIfNull(abstraction);
        ArgumentNullException.ThrowIfNull(assetLocator);
        ArgumentNullException.ThrowIfNull(audioOptions);

        _abstraction = abstraction;
        Graphics = abstraction.Graphics;
        Layout = abstraction.Layout;
        Keyboard = abstraction.Keyboard;
        Gamepad = abstraction.Gamepad;
        Sound = abstraction.Sound;
        AudioOptions = audioOptions;
    }

    public bool IsRunning { get; private set; } = true;

    internal IGraphics Graphics { get; }

    internal ScreenLayout Layout { get; }

    internal IKeyboard Keyboard { get; }

    internal IGamepad Gamepad { get; }

    internal ISound Sound { get; }

    internal AudioOptions AudioOptions { get; }

    public void Run() => GameHost.Run(_abstraction, this, TickRate, TickRate);

    public void Update()
    {
        if (Keyboard.IsPressed(ConsoleKey.Escape))
        {
            IsRunning = false;
        }
    }

    public void Draw()
    {
        Graphics.Clear();
        Graphics.ScreenUpdate();
    }
}
