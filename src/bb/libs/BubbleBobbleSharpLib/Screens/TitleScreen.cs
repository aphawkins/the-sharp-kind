// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using SharpKind.Abstraction;

namespace BubbleBobbleSharpLib.Screens;

// $4556-$4590: the title screen. The tune plays, and the game starts when fire is pressed on either joystick.
internal sealed class TitleScreen : IGameScreen
{
    // The rows above the first line of credits, which the banner covers.
    internal const int LogoRows = 7;

    private const int Fire = 0x10;

    private readonly Func<byte> _fire;
    private readonly Func<bool> _superKeys;
    private readonly Action<bool> _start;
    private readonly Action<Song> _play;
    private readonly Action<TitleModel> _draw;

    internal TitleScreen(Func<byte> fire, Func<bool> superKeys, Action<bool> start, Action<Song> play, Action<TitleModel> draw)
    {
        ArgumentNullException.ThrowIfNull(fire);
        ArgumentNullException.ThrowIfNull(superKeys);
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(play);
        ArgumentNullException.ThrowIfNull(draw);

        _fire = fire;
        _superKeys = superKeys;
        _start = start;
        _play = play;
        _draw = draw;
    }

    // $462D-$47B4. The reference writes a full stop as $5B and an exclamation mark as $5C.
    internal static TitleModel Model { get; } = new([
        new(7, 7, 5, "WRITTEN BY  STEPHEN RUDDY."),
        new(5, 9, 7, "GRAPHICS BY  ANDREW THRELFALL."),
        new(8, 12, 4, "COMMODORE CONVERSION  BY"),
        new(11, 13, 4, "SOFTWARE CREATIONS"),
        new(9, 15, 2, "PRODUCED BY  FIREBIRD."),
        new(10, 18, 5, "LICENSED BY FIREBIRD"),
        new(8, 19, 5, "FROM  TAITO ELECTRONICS."),
        new(4, 23, 1, "! PRESS FIRE FOR BUBBLE BOBBLE !"),
    ]);

    public void Reset() => _play(Song.Title);

    // $4568-$4590, $45A7-$45E1: six keys held together, on the keyboard and not the stick, are the cheat, which the game starts in.
    public void Update()
    {
        if (_superKeys())
        {
            _start(true);
            return;
        }

        if ((_fire() & Fire) == 0)
        {
            _start(false);
        }
    }

    public void Draw() => _draw(Model);
}
