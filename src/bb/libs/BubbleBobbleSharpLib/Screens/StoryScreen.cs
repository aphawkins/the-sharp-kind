// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharpLib.Levels;
using SharpKind;
using SharpKind.Abstraction;

namespace BubbleBobbleSharpLib.Screens;

// $08E4-$091B: the story, under which the level's theme starts. $0913 waits 255 frames ($7BC8) before the level is drawn,
// which VICE shows as 255 frames from the text to $091B's clear: 128 of the port's passes at 25 a second.
internal sealed class StoryScreen : IGameScreen
{
    internal const int Passes = 128;

    // $08F3-$0906: both players' dragons in bubbles, the second whether it is in the game or not ($0904).
    private const byte BothPlayers = 0x03;

    // A pass is two frames of the C64's.
    private const int FramesPerPass = 2;

    private static readonly byte[] s_x = [0x74, 0xE4, 0, 0, 0, 0, 0, 0];
    private static readonly byte[] s_y = [0x83, 0x83, 0, 0, 0, 0, 0, 0];

    private readonly IRandomSource _random;
    private readonly Action _songs;
    private readonly Action _play;
    private readonly Action<TitleModel> _draw;
    private readonly Action<SpriteModel> _drawSprites;

    private int _passes;
    private TitleModel _screen = Model;

    internal StoryScreen(IRandomSource random, Action songs, Action play, Action<TitleModel> draw, Action<SpriteModel> drawSprites)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(songs);
        ArgumentNullException.ThrowIfNull(play);
        ArgumentNullException.ThrowIfNull(draw);
        ArgumentNullException.ThrowIfNull(drawSprites);

        _random = random;
        _songs = songs;
        _play = play;
        _draw = draw;
        _drawSprites = drawSprites;
    }

    // $AC41-$ACB5. The reference writes a comma as $5E, an apostrophe as $5F and an exclamation mark as $5C.
    internal static TitleModel Model { get; } = new([
        new(5, 0, 7, "NOW, IT IS THE BEGINNING OF A"),
        new(5, 2, 7, "FANTASIC STORY!! LET'S MAKE A"),
        new(4, 4, 7, "JOURNEY TO THE CAVE OF MONSTERS!"),
        new(13, 6, 7, "! GOOD  LUCK !"),
    ]);

    // $0885's stars, and $08E7's theme, the one the new game has asked for.
    public void Reset()
    {
        _passes = 0;
        _screen = Starfield.Scatter(_random, Model);
        _songs();
    }

    public void Update()
    {
        if (++_passes >= Passes)
        {
            _play();
        }
    }

    public void Draw()
    {
        _draw(_screen);
        _drawSprites(Dragons(_passes));
    }

    // $090E turns the split on before $0918's wait, so the IRQ turns the bubbles' frame every eight frames ($0746).
    internal static SpriteModel Dragons(int passes)
        => BubbleGlide.Bubbles(BothPlayers, s_x, s_y, BubbleGlide.BubbleFrame(passes * FramesPerPass));
}
