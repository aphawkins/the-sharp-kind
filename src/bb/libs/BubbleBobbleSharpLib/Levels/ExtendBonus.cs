// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Levels;

// $32C1-$3420 (`extend-bonus.s`): a player who has all six EXTEND letters (`$54`, `$55` at `$3F`) gets a life, loses the
// letters, and the level ends. Player 2 is looked at first. The fanfare (song 7) is asked for, and the game waits in itself
// with the pause flag up while the IRQ keeps the clock: the routine is left in `Hold` for the game to run. After it,
// `jmp $32C3` looks at the same player again and then the one below, so two players with their letters each get a hold.
internal sealed class ExtendBonus
{
    private const byte AllLetters = 0x3F;

    private readonly PlayerTable _players;
    private readonly LevelEnd _end;
    private readonly Action _resetObjects;
    private readonly BbRandom _random;

    // The player the look goes on from, after a hold.
    private int _next = PlayerTable.Capacity - 1;

    internal ExtendBonus(PlayerTable players, LevelEnd end, Action resetObjects, BbRandom random)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(end);
        ArgumentNullException.ThrowIfNull(resetObjects);
        ArgumentNullException.ThrowIfNull(random);

        _players = players;
        _end = end;
        _resetObjects = resetObjects;
        _random = random;
    }

    // How many times a player has earned the life, for the song ($32C1's fanfare).
    internal int Awards { get; private set; }

    // The wait a player's letters have started, for the game to run before it goes on with the pass, or none.
    internal ExtendHold? Hold { get; set; }

    // The wait that is going on, until it is over.
    internal ExtendHold? Running { get; private set; }

    // The screen shown in place of the level while a wait is going on, or none.
    internal ExtendPicture? Picture(int tile, int colours) => Running is { Screen: ExtendScreen screen } hold
        ? new(screen, hold.Player, tile, colours, hold.Art, hold.ColourRam)
        : null;

    internal void Update()
    {
        for (int player = _next; player >= 0; player--)
        {
            if (_players.ExtendLetters[player] != AllLetters)
            {
                continue;
            }

            _next = player - 1;
            Awards++;
            Hold = new ExtendHold(this, player);
            Running = Hold;
            return;
        }

        _next = PlayerTable.Capacity - 1;
    }

    // `$32CD`-`$3420`, in IRQs from the call, the first taken by the fanfare's start (`$05AD`): the pause flag up (`$32D3`); the
    // player's six letters in the sidebar drawn again at random twenty times, each after five frames (`$32EE`); the objects
    // reset (`$2E79`) and the screen cleared (`$E374`); fifteen frames (`$3346`); the screen of fruit and the EXTEND banner,
    // shown at 128 (`$33EF`); 200 frames (`$33F4`); eleven turns of the screens, eleven frames each (`$33FC`); the screen
    // cleared and ten frames (`$3407`). Then the life, the letters cleared, and the level skipped (`$340F`-`$341A`).
    // What is shown, as the real game shows it: the level with the rolled letters to the reset; black, with no sprites,
    // from the clear; the fruit from 127 (its last two frames before the banner are the other screen's, empty, which this
    // does not draw); the text and the fruit by turns from 329; black from the last clear. Each is what the frame's work leaves for the next IRQ. The fill takes the frames from 125
    // to 127, as long as its random numbers take; here it is all done at 125.
    internal sealed class ExtendHold(ExtendBonus bonus, int player) : IGameHold
    {
        internal const int Length = 460;

        private const int FirstRoll = 1;
        private const int RollFrames = 5;
        private const int Rolls = 20;
        private const int ResetFrame = FirstRoll + (Rolls * RollFrames);
        private const int ClearFrame = 107;

        // `$2E79`'s drawing of the level (`$2EC0`), two frames into it, onto both screens in turn: the one with the rolled
        // letters is shown for a frame more, two frames on.
        private const int RedrawFrame = 103;
        private const int OldScreenFrame = RedrawFrame + 2;
        private const int FillFrame = 125;
        private const int FruitFrame = 127;
        private const int TurnsFrame = 329;
        private const int TurnFrames = 11;
        private const int LastClearFrame = 449;

        // `$32F5` and `$338D`: the random numbers past these are drawn again.
        private const int LetterPictures = 5;
        private const int LetterMask = 0x07;
        private const int FoodMask = 0x1F;
        private const byte LastFruitArt = 0x27;
        private const int FirstArtCharacter = 0x60;

        private readonly int[] _roll = new int[SidebarModel.LetterCount];
        private readonly byte[] _art = new byte[ExtendPicture.FruitColumns * ExtendPicture.FruitRows];
        private readonly byte[] _colourRam = new byte[ExtendPicture.FruitColumns * ExtendPicture.FruitRows];

        private bool _rolled;

        public bool Paused => true;

        public int? Border => null;

        internal int Player => player;

        // IRQs since the call.
        internal int Frames { get; private set; }

        // The letter pictures in the player's six places, or none before the first roll and once the level is drawn again.
        internal IReadOnlyList<int>? Roll => _rolled && Frames is < RedrawFrame or OldScreenFrame ? _roll : null;

        // `$2E79` draws the level again, without the player's letters, and it stays so until the screen is cleared.
        internal bool Unlettered => Frames is >= RedrawFrame and < ClearFrame and not OldScreenFrame;

        // The screen shown in place of the level, or none while the level is.
        internal ExtendScreen? Screen => Frames switch
        {
            < ClearFrame => null,
            < FruitFrame => ExtendScreen.Cleared,
            < TurnsFrame => ExtendScreen.Fruit,
            < LastClearFrame => int.IsEvenInteger((Frames - TurnsFrame) / TurnFrames) ? ExtendScreen.Text : ExtendScreen.Fruit,
            _ => ExtendScreen.Cleared,
        };

        internal ReadOnlySpan<byte> Art => _art;

        internal ReadOnlySpan<byte> ColourRam => _colourRam;

        public bool Frame()
        {
            if (Frames == Length)
            {
                return false;
            }

            Frames++;

            if (Frames > FirstRoll && Frames <= ResetFrame && (Frames - FirstRoll) % RollFrames == 0)
            {
                RollLetters();
            }

            if (Frames == ResetFrame)
            {
                bonus._resetObjects();
            }

            if (Frames == FillFrame)
            {
                Fill();
            }

            return true;
        }

        public void Pass()
        {
        }

        public byte Finish(byte counter)
        {
            bonus._players.Lives[player]++;
            bonus._players.ExtendLetters[player] = 0;
            bonus._end.Skip();
            bonus.Running = null;
            return counter;
        }

        // `$32F5`: a picture of E, X, T, N or D in each place, from row 10 down.
        private void RollLetters()
        {
            for (int place = 0; place < _roll.Length; place++)
            {
                int picture;

                do
                {
                    picture = bonus._random.Next() & LetterMask;
                }
                while (picture >= LetterPictures);

                _roll[place] = picture;
            }

            _rolled = true;
        }

        // `$338D`: a food at random for each block, row by row, again for any whose art is past the fruit. `$3366` has copied
        // the item art to characters `$60` on, four to a block, but the block's character is its art times four ORed, not
        // added, with `$60`: art under `$20` with bit 3 or 4 set shows another block.
        private void Fill()
        {
            for (int block = 0; block < _art.Length; block++)
            {
                int food;

                do
                {
                    food = bonus._random.Next() & FoodMask;
                }
                while (LevelItems.FoodArt(food) > LastFruitArt);

                _art[block] = (byte)((((LevelItems.FoodArt(food) << 2) | FirstArtCharacter) - FirstArtCharacter) >> 2);
                _colourRam[block] = LevelItems.FoodColour(food);
            }
        }
    }
}
