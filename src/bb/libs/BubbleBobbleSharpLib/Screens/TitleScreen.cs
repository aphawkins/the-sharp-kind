// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using SharpKind.Abstraction;

namespace BubbleBobbleSharpLib.Screens;

// $4556-$4590: the title screen. The tune plays, and the game starts when fire is pressed on either joystick.
internal sealed class TitleScreen : IGameScreen
{
    private const int Fire = 0x10;

    private static readonly string[] s_logo =
    [
        Logo("80 81 82 40 40 40 80 81 82 80 81 82 93 40 40 97 98 99 40 40 40 40 80 81 82 40 40 40 80 81 82 80 81 82 93 40 40 97 98 99"),
        Logo("83 84 85 8C 40 8D 83 84 85 83 84 85 8E 40 40 9A 9B 9C 40 40 40 40 83 84 85 A3 A4 A5 83 84 85 83 84 85 8E 40 40 9A 9B 9C"),
        Logo("86 87 88 8E 40 8F 86 87 88 86 87 88 8E 40 40 9D 9E 9F 40 40 40 40 86 87 88 A6 A7 A8 86 87 88 86 87 88 8E 40 40 9D 9E 9F"),
        Logo("89 8A 8B 90 91 92 89 8A 8B 89 8A 8B 94 95 96 A0 A1 A2 40 40 40 40 89 8A 8B A9 AA AB 89 8A 8B 89 8A 8B 94 95 96 A0 A1 A2"),
    ];

    private readonly Func<byte> _fire;
    private readonly Action _start;
    private readonly Action<Song> _play;
    private readonly Action<TitleModel> _draw;

    internal TitleScreen(Func<byte> fire, Action start, Action<Song> play, Action<TitleModel> draw)
    {
        ArgumentNullException.ThrowIfNull(fire);
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(play);
        ArgumentNullException.ThrowIfNull(draw);

        _fire = fire;
        _start = start;
        _play = play;
        _draw = draw;
    }

    // $462D-$47B4. The reference writes a full stop as $5B and an exclamation mark as $5C.
    internal static TitleModel Model { get; } = new(
        s_logo,
        LogoColour: 11,
        [
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

    public void Update()
    {
        if ((_fire() & Fire) == 0)
        {
            _start();
        }
    }

    public void Draw() => _draw(Model);

    // One row of the logo: the bytes the reference writes, less $20 for the screen code and less $60 for the glyph.
    private static string Logo(string bytes)
    {
        const int First = 0x80;

        return string.Concat(bytes.Split(' ').Select(hex =>
        {
            int code = Convert.ToInt32(hex, 16);
            return code < First ? TitleModel.LogoBlank : (char)(' ' + (code - First));
        }));
    }
}
