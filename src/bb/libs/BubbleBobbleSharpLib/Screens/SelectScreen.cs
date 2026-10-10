// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Globalization;
using BubbleBobbleSharp.Abstractions.Views;
using SharpKind;
using SharpKind.Abstraction;

namespace BubbleBobbleSharpLib.Screens;

// $F005-$F0E3: the last game's scores and rounds, and the top score. The keyboard's 1 or 2 starts a game for that many.
internal sealed class SelectScreen : IGameScreen
{
    // $F085: where each player's score and round go, and the top score.
    private const int ScoreColumn = 15;
    private const int RoundColumn = 27;
    private const int TopColumn = 15;
    private const int TopRow = 14;
    private const int TopColour = 1;

    // $A5A0: the rules above the scores and below the top score.
    private const int RuleColumn = 3;
    private const int RuleLength = 34;

    // $F01A-$F042: the players' dragons, sprites 0 and 1 at the first sprite pointer, one above the other.
    private const byte BothDragons = 0x03;
    private const byte FirstPointer = 0x60;

    private static readonly int[] s_rows = [8, 11];
    private static readonly int[] s_colours = [5, 3];

    private static readonly byte[] s_dragonX = [0x68, 0x68, 0, 0, 0, 0, 0, 0];
    private static readonly byte[] s_dragonY = [0x65, 0x7D, 0, 0, 0, 0, 0, 0];
    private static readonly byte[] s_dragonColours = [0x05, 0x03, 0, 0, 0, 0, 0, 0];

    private readonly IRandomSource _random;
    private readonly Func<bool> _one;
    private readonly Func<bool> _two;
    private readonly Action<int> _start;
    private readonly Func<TitleModel> _model;
    private readonly Action<TitleModel> _draw;
    private readonly Action<SpriteModel> _drawSprites;

    private IReadOnlyList<TitleStar> _stars = [];

    internal SelectScreen(
        IRandomSource random,
        Func<bool> one,
        Func<bool> two,
        Action<int> start,
        Func<TitleModel> model,
        Action<TitleModel> draw,
        Action<SpriteModel> drawSprites)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(one);
        ArgumentNullException.ThrowIfNull(two);
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(draw);
        ArgumentNullException.ThrowIfNull(drawSprites);

        _random = random;
        _one = one;
        _two = two;
        _start = start;
        _model = model;
        _draw = draw;
        _drawSprites = drawSprites;
    }

    // $F0EE-$F191. The reference writes the exclamation mark as $5C, and 1 and 2 as $21 and $22.
    internal static IReadOnlyList<TitleText> Text { get; } = [
        new(11, 0, 7, "! BUBBLE  BOBBLE !"),
        new(8, 17, 7, "COMMODORE CONVERSION  BY"),
        new(11, 18, 7, "SOFTWARE CREATIONS"),
        new(16, 6, 4, "SCORE"),
        new(25, 6, 4, "ROUND"),
        new(10, 14, 2, "TOP"),
        new(8, 20, 3, "WRITTEN BY STEPHEN RUDDY"),
        new(29, 24, 3, "CREDITS 10"),
        new(8, 22, 1, "! PRESS 1 OR 2 TO PLAY !"),

        // $F012-$F01F, $A63A: the scores' strip, spaces but for the zero every score ends in, which the scores go over.
        new(15, 8, 5, "     00       "),
        new(15, 11, 3, "     00       "),
        new(15, 14, 1, "     00       "),
    ];

    internal static IReadOnlyList<TitleRule> Rules { get; } = [new(RuleColumn, 2, RuleLength), new(RuleColumn, 15, RuleLength)];

    internal static SpriteModel Dragons { get; } = BuildDragons();

    // $A5A0, $0885: new stars each time the screen is shown.
    public void Reset() => _stars = Starfield.Scatter(_random, new(Text) { Rules = Rules }).Stars;

    // $F0CC-$F0E3: 1 is checked first.
    public void Update()
    {
        if (_one())
        {
            _start(1);
        }
        else if (_two())
        {
            _start(2);
        }
    }

    public void Draw()
    {
        _draw(_model() with { Stars = _stars });
        _drawSprites(Dragons);
    }

    // $F085-$F0BE: the last game's scores and the rounds its players reached, and the top score and its round.
    internal static TitleModel Build(HudModel hud, ReadOnlySpan<byte> rounds, byte topRound)
    {
        ArgumentNullException.ThrowIfNull(hud);

        List<TitleText> lines = [.. Text];
        string[] scores = [hud.PlayerOneScore, hud.PlayerTwoScore];

        for (int player = 0; player < scores.Length; player++)
        {
            lines.Add(new(ScoreColumn, s_rows[player], s_colours[player], scores[player]));
            lines.Add(new(RoundColumn, s_rows[player], s_colours[player], Round(rounds[player])));
        }

        lines.Add(new(TopColumn, TopRow, TopColour, hud.HighScore));
        lines.Add(new(RoundColumn, TopRow, TopColour, Round(topRound)));

        return new(lines) { Rules = Rules };
    }

    // $F0AE: a round is kept as the level before it, and shown as at least two digits.
    private static string Round(byte round) => (round + 1).ToString("D2", CultureInfo.InvariantCulture);

    private static SpriteModel BuildDragons()
    {
        byte[] none = new byte[SpriteModel.Capacity];
        byte[] pointers = new byte[SpriteModel.Capacity];

        Array.Fill(pointers, FirstPointer);

        return new(none, s_dragonX, s_dragonY, none, pointers, s_dragonColours, none, BothDragons);
    }
}
