// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Levels;

// What the clock running out does ($15E1-$15FA): the hurry-up, the Baron, or, on a level that has been cleared,
// the end of the level.
//
// The first time, $2D is 0: the hurry-up makes every enemy angry and gives the players ten more seconds. The
// second, with $2D at 1, is the Baron: one for each player in the game, in the object slot of the player's number,
// and the clock stopped. On a cleared level, with $2D negative, it is the end of the level.
internal sealed class LevelEnd
{
    private const byte HurryUpSeconds = 0x0A;
    private const byte AngryFully = 0xFF;

    private const byte PopType = 0x3A;
    private const byte FirstBaronType = 0x2E;
    private const byte BaronFoodSeed = 0xFF;

    private static readonly byte[] s_spawnColour = [0x05, 0x03];

    private static readonly byte[] s_spawnColumn = [0x02, 0x1B];
    private static readonly byte[] s_spawnRow = [0x05, 0x19];

    private readonly LevelTimer _timer;
    private readonly EntityTable _entities;
    private readonly ObjectTable _objects;
    private readonly SlotPop _slots;
    private readonly LevelItems _items;
    private readonly BbRandom _random;
    private readonly RoundState _round;

    internal LevelEnd(
        LevelTimer timer,
        EntityTable entities,
        ObjectTable objects,
        BubblePop pop,
        LevelItems items,
        BbRandom random,
        RoundState? round = null)
    {
        ArgumentNullException.ThrowIfNull(timer);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(pop);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(random);

        _timer = timer;
        _entities = entities;
        _objects = objects;
        _slots = new(objects, pop);
        _items = items;
        _random = random;
        _round = round ?? new();
    }

    internal static ReadOnlySpan<byte> SpawnColour => s_spawnColour;

    // $21: the level is over, and $0A6A takes the game on to the next.
    internal byte Complete { get; private set; }

    // $A813, $A814: the flash a player had when a bonus round began, and has again after it. The bonus
    // round (step 7) is what sets them.
    internal byte[] SavedFlash { get; } = new byte[PlayerTable.Capacity];

    // `$1621`: the Barons' call puts the giant bubble's sweeps (`$58FF`) to nothing.
    internal GiantBubble? Giant { get; set; }

    // `$1621`'s waits, left for the game to run once the clock has run out a second time.
    internal IGameHold? Hold { get; set; }

    // `$05AD`, for the songs the hurry-up's scroll asks for as it goes.
    internal Action<Song>? Play { get; set; }

    // The last hurry-up's scroll, for its text.
    internal HurryHold? Hurrying { get; private set; }

    // How many times the Barons have been called, for the song ($1621's `ldy #$43`, the round start's).
    internal int BaronCalls { get; private set; }

    // $05C5 clears $21 at each level's start.
    internal void Begin()
    {
        Complete = 0;
        Hurrying = null;
    }

    // $32E9's `dec $21` (`ExtendBonus`): the level is over, whatever the enemies say.
    internal void Skip() => Complete--;

    // $15E4-$15FA.
    internal void Update()
    {
        if (_timer.Hurry == 0)
        {
            HurryUp();
        }
        else if ((sbyte)_timer.Hurry > 0)
        {
            SpawnBarons();
        }
        else
        {
            Finish();
        }
    }

    // $15E8-$15F4. The HURRY UP! scroll (`$3AB8`) waits in itself, with the pause flag up: the routine is left in `Hold` for
    // the game to run, a frame at a time, and the hurry-up and its ten seconds come once it is over.
    private void HurryUp()
    {
        Hurrying = new(this);
        Hold = Hurrying;
    }

    // $1621-$1693. Whatever is in the Barons' two object slots pops first, and the game waits for it to go. Then
    // each slot has a column and a row chosen by chance, and the slot of each player in the game gets a Baron there.
    // The clock stops, with the level's time on it, until the last Baron goes ($1490).
    //
    // The waits are the game's own, with the pause flag up ($15E5): the routine is left in `Hold` for the game to run, a
    // frame at a time, and the last waits for the round start's tune to end ($1685).
    private void SpawnBarons()
    {
        BaronCalls++;
        _slots.Pop();
        Hold = new BaronHold(this);
    }

    // `$1633`-`$1666`, once the slots are free: a place for each slot, and a pop in the slot of each player in the game.
    private void PlaceBarons(Span<byte> column, Span<byte> row)
    {
        Giant?.Cancel();

        _items.FoodBase = BaronFoodSeed;

        for (int slot = PlayerTable.Capacity - 1; slot >= 0; slot--)
        {
            _objects.Slot(slot).SubY = 0;
            _objects.Slot(slot).SubX = 0;
            _objects.Slot(slot).Variant = 0;
            _objects.Slot(slot).EnemyType = 0;

            column[slot] = s_spawnColumn[_random.Next() & 1];
            _objects.Slot(slot).Column = column[slot];
            row[slot] = s_spawnRow[_random.Next() & 1];
            _objects.Slot(slot).Row = row[slot];

            if (_entities.Slot(slot).State != 0)
            {
                _objects.Slot(slot).Type = PopType;
            }
        }
    }

    // `$1669`-`$1693`, after the second wait: the Barons, and the clock stopped.
    private void BringBarons(ReadOnlySpan<byte> column, ReadOnlySpan<byte> row)
    {
        for (int slot = PlayerTable.Capacity - 1; slot >= 0; slot--)
        {
            if (_entities.Slot(slot).State != 0)
            {
                _objects.Slot(slot).Column = column[slot];
                _objects.Slot(slot).Row = row[slot];
                _objects.Slot(slot).Type = (byte)(FirstBaronType + (slot << 1));
            }
        }

        _timer.Frames = LevelTimer.Stopped;
        _timer.Hurry = 0;
        _timer.Seconds = _timer.Start;
    }

    // $15FA-$1620. Each player in the game, player 2 first, has to be playing or still flashing from a respawn, and
    // one flashing goes on to play. A player in any other state makes the level wait a pass; otherwise it is over.
    private void Finish()
    {
        for (int player = PlayerTable.Capacity - 1; player >= 0; player--)
        {
            byte state = _entities.Slot(player).State;

            if (state is 0 or PlayerFrame.PlayingState)
            {
                continue;
            }

            if (state != _round.Respawn)
            {
                return;
            }

            _entities.Slot(player).State = PlayerFrame.PlayingState;
            _entities.Slot(player).FlashTimer = SavedFlash[player];
            _entities.Slot(player).Colour = s_spawnColour[player];
        }

        Complete++;
    }

    // `$3AB8`-`$3BFE`: a frame and the anger (`$16E4`); the text up a row a frame to row 11, where the hurry-up's tune starts and
    // the text flashes six times eight frames (`$3B93`); the wait for the tune to end (`$3BB1`), and the level's tune again
    // (song 6, `$3BB7`); then the text up the rest of the way, a row a frame, a last frame, and the sidebar's text redrawn. The
    // songs' starts take a frame each.
    internal sealed class HurryHold(LevelEnd end) : IGameHold
    {
        // The frame and the 14 rows to row 11.
        private const int TuneFrame = 1 + 14;

        private const int FlashFrames = 6 * 8;

        // How long `$F960`'s tune keeps `$A4` up, in IRQ frames, as the sound driver plays it.
        private const int TuneFrames = 86;

        // The frame song 6's start takes, rows 10 to 0, and the last frame.
        private const int AfterFrames = 1 + 11 + 1;

        // Row 11's last frame.
        private const int HeldFrame = EndFrame + 1;

        private const int EndFrame = TuneFrame + (FlashFrames > TuneFrames ? FlashFrames : TuneFrames);

        private const int FirstRow = 24;
        private const int TuneRow = 11;
        private const int FlashLength = 8;
        private const int Flashes = 6;

        private const byte White = 0x0F;
        private const byte Grey = 0x0C;

        private int _frames;

        public bool Paused => true;

        // The screen row the text is on, or none before it is drawn and once `$3BD8` has put the level back.
        public int? Row => _frames switch
        {
            >= 2 and < TuneFrame => FirstRow + 2 - _frames,
            >= TuneFrame and <= HeldFrame => TuneRow,
            > HeldFrame and <= HeldFrame + TuneRow => HeldFrame + TuneRow - _frames,
            _ => null,
        };

        // Row 11's text is cleared around its letters to black (`$3B8B`).
        public bool Backed => _frames is >= TuneFrame and <= HeldFrame;

        // The text's colour RAM: white (`$3B39`), and at row 11 grey EOR 3 eight frames at a time (`$3B9A`), grey last. The
        // flashes start a frame after the tune does, which music_start takes.
        public byte ColourRam
        {
            get
            {
                int flashes = Math.Min((_frames - TuneFrame - 1) / FlashLength, Flashes);

                return Backed && flashes > 0 && (flashes & 1) == 0 ? Grey : White;
            }
        }

        public int? Border => null;

        public bool Frame()
        {
            if (_frames == EndFrame + AfterFrames)
            {
                return false;
            }

            _frames++;

            switch (_frames)
            {
                case 1:
                    end._entities.SetAnger(AngryFully);
                    break;
                case TuneFrame:
                    end.Play?.Invoke(Song.HurryUp);
                    break;
                case EndFrame:
                    end.Play?.Invoke(Song.LevelComplete);
                    break;
            }

            return true;
        }

        public void Pass()
        {
        }

        public byte Finish(byte counter)
        {
            end._timer.Hurry++;
            end._timer.Seconds = HurryUpSeconds;
            return counter;
        }
    }

    // `$1B02`-`$1B18` twice: an object update a frame, at least one, until both slots are free; the Barons' places are
    // chosen between the two waits. Then `$1685` waits for the round start's tune, asked for on the call's frame, to end, and
    // the level's tune (song 6, `$1688`) is asked for again; its start takes a frame.
    private sealed class BaronHold(LevelEnd end) : IGameHold
    {
        // How long song `$43` keeps `$A4` up, in IRQ frames from its ask, as the sound driver plays it.
        private const int TuneFrames = 107;

        private readonly byte[] _column = new byte[PlayerTable.Capacity];
        private readonly byte[] _row = new byte[PlayerTable.Capacity];

        private int _waits = 2;
        private int _frames;
        private int _asked;

        public bool Paused => true;

        public int? Border => null;

        public bool Frame()
        {
            if (_asked != 0 && _frames > _asked)
            {
                return false;
            }

            _frames++;

            if (_waits > 0)
            {
                end._slots.Update();

                if (end._slots.Free())
                {
                    _waits--;

                    if (_waits == 1)
                    {
                        end.PlaceBarons(_column, _row);
                    }
                }

                if (_waits > 0)
                {
                    return true;
                }
            }

            if (_asked == 0 && _frames >= TuneFrames)
            {
                _asked = _frames;
                end.Play?.Invoke(Song.LevelComplete);
            }

            return true;
        }

        public void Pass()
        {
        }

        public byte Finish(byte counter)
        {
            end.BringBarons(_column, _row);
            return counter;
        }
    }
}
