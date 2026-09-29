// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Levels;

// The end of the game (`$A5B7`-`$A624`, from `$0AA8` when the last level is cleared): the player who touched the stunned giant
// is paid a million, a hundred frames' worth of `$10` in the ten thousands (`$7C26`); the level stays on the screen for fifty
// frames more; and then the screen goes blank but for three lines of text, the ending's song plays and the giant walks the
// screen (`$0840`) until fire has been pressed and let go. The game waits a frame a step in `$E494`, and a step here is two.
internal sealed class GameEnding
{
    private const int PayPasses = 100 / 2;
    private const int WaitPasses = 0x32 / 2;
    private const byte PayEach = 0x10;
    private const int FramesAPass = 2;
    private const byte FireBit = 0x10;
    private const byte OnlyTheGiant = 0xFC;

    private static readonly RoomText[] s_lines =
    [
        new(12, 5, 7, "CONGRATULATIONS"),
        new(4, 8, 5, "YOU HAVE COMPLETED BUBBLE BOBBLE"),
        new(12, 11, 1, "< WHAT HEROES <"),
    ];

    private readonly EntityTable _entities;
    private readonly Scores _scores;
    private readonly BossFight _boss;
    private readonly Action<Song> _play;

    private Stage _stage = Stage.Idle;
    private int _passes;
    private bool _pressed;

    internal GameEnding(EntityTable entities, Scores scores, BossFight boss, Action<Song> play)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(scores);
        ArgumentNullException.ThrowIfNull(boss);
        ArgumentNullException.ThrowIfNull(play);

        _entities = entities;
        _scores = scores;
        _boss = boss;
        _play = play;
    }

    private enum Stage
    {
        Idle = 0,
        Paying = 1,
        Waiting = 2,
        Showing = 3,
        Done = 4,
    }

    // The game has ended, and takes no more passes of the level.
    internal bool Active => _stage != Stage.Idle;

    // The screen is blank but for the ending's text and the giant.
    internal bool Blank => _stage is Stage.Showing or Stage.Done;

    // Fire was pressed and let go: the game goes back to the front end.
    internal bool Finished => _stage == Stage.Done;

    internal IReadOnlyList<RoomText> Lines => Blank ? s_lines : [];

    // `$0AA8`.
    internal void Begin()
    {
        _stage = Stage.Paying;
        _passes = 0;
        _pressed = false;
    }

    // One pass, with the joysticks' ports read as the game reads them: a bit is clear when it is pressed.
    internal void Step(ReadOnlySpan<byte> ports)
    {
        switch (_stage)
        {
            case Stage.Paying:
                for (int frame = 0; frame < FramesAPass; frame++)
                {
                    _scores.Add(Scores.Last(_boss.Winner) - 1, PayEach);
                }

                Next(PayPasses, Stage.Waiting);
                break;

            case Stage.Waiting:
                Next(WaitPasses, Stage.Showing);

                if (_stage == Stage.Showing)
                {
                    Show();
                }

                break;

            case Stage.Showing:
                for (int frame = 0; frame < FramesAPass; frame++)
                {
                    _boss.Parade();
                }

                WaitForFire(ports);
                break;
        }
    }

    private void Next(int passes, Stage stage)
    {
        _passes++;

        if (_passes < passes)
        {
            return;
        }

        _passes = 0;
        _stage = stage;
    }

    // `$A5A0`-`$A5B6` and what follows it: the level is wiped from the screen, the text is written and the giant is put out.
    private void Show()
    {
        _entities.SpriteEnable = OnlyTheGiant;
        _boss.Present();
        _play(Song.Ending);
    }

    // `$A5EB`-`$A624`: fire on either port, and then neither.
    private void WaitForFire(ReadOnlySpan<byte> ports)
    {
        bool fire = (ports[0] & FireBit) == 0 || (ports[1] & FireBit) == 0;

        if (!_pressed)
        {
            _pressed = fire;
        }
        else if (!fire)
        {
            _stage = Stage.Done;
        }
    }
}
