// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Tests.Oracle;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// What the EXTEND bonus (`$32CD`) shows, against the real game run on SharpKind.Emulation: player 1's letters are given at
// `$32C1` in a level 1 pass (an intervention), and the screen the VIC shows (`$D018`) is read at each IRQ to the routine's
// return. The game's random numbers are given to the port, so its rolls and its fruit are the game's. Compared at each IRQ:
// which screen is shown, the rolled letters on the left wall, and the fruit outside the box.
[Trait("Level", "Integration")]
public sealed class ExtendPictureOracleTests
{
    private const int Counter = 0x08;
    private const int Letters = 0x54;
    private const int RandomLow = 0x26;
    private const int RandomHigh = 0x27;
    private const int RandomReturn = 0xE9FC;
    private const int Return = 0x0A5A;
    private const int Bank = 0x4000;
    private const int Columns = 40;

    private const byte Space = 0x20;
    private const byte FirstLetter = 0x4A;
    private const byte FirstFruit = 0x60;
    private const byte FirstText = 0x21;

    // The frame before the banner, on the other screen, which the port shows as the fruit (see `ExtendHold`).
    private const int Undrawn = 127;

    private static readonly System.Globalization.CultureInfo s_culture = System.Globalization.CultureInfo.InvariantCulture;

    [Fact]
    public void ShowsWhatTheGameShows()
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = new();
        machine.StartOnePlayer();
        machine.RunTo(0x32C1, 2_000_000);
        machine.Poke(Letters, 0x3F);

        (byte low, byte high) = (machine.Peek(RandomLow), machine.Peek(RandomHigh));
        List<Shown> game = Run(machine, out List<byte> randoms);

        EndRig rig = new(ScriptedRandomSource.For(low, high, randoms));
        rig.Random.Low = low;
        rig.Random.High = high;
        rig.Players.ExtendLetters[0] = 0x3F;

        ExtendBonus extend = new(rig.Players, rig.End, () => { }, rig.Random);
        extend.Update();
        ExtendBonus.ExtendHold hold = extend.Hold!;

        Assert.Equal(ExtendBonus.ExtendHold.Length, game.Count);

        // What a frame's work leaves is what the next IRQ finds.
        for (int frame = 1; frame <= game.Count; frame++)
        {
            Assert.True(hold.Frame());

            if (frame == Undrawn || frame == game.Count)
            {
                continue;
            }

            Shown shown = game[frame];
            Assert.True(PortKind(hold) == shown.Kind, $"IRQ {frame}: the game shows {shown.Kind}, the port {PortKind(hold)}.");

            if (shown.Kind == "level")
            {
                Assert.True(PortRoll(hold) == shown.Roll, $"IRQ {frame}: the game rolls {shown.Roll}, the port {PortRoll(hold)}.");
            }

            if (shown.Kind == nameof(ExtendScreen.Fruit))
            {
                Assert.Equal(shown.Fruit, PortFruit(hold));
            }
        }

        Assert.False(hold.Frame());
    }

    // From the call to its return: what the VIC shows by each IRQ, from the first, and the random numbers `$E9EA` gave.
    private static List<Shown> Run(Rebb64Machine machine, out List<byte> randoms)
    {
        byte counter = machine.Peek(Counter);
        long limit = machine.Cycles + (985_248L * 20);
        List<Shown> shown = [];
        int last = 0;

        randoms = [];

        while (machine.Cpu.PC != Return)
        {
            if (machine.Cpu.PC == RandomReturn)
            {
                randoms.Add(machine.Cpu.A);
            }

            if ((byte)(machine.Peek(Counter) - counter) != last)
            {
                last = (byte)(machine.Peek(Counter) - counter);
                shown.Add(Read(machine));
            }

            machine.Step();
            Assert.True(machine.Cycles < limit, "The bonus did not return.");
        }

        return shown;
    }

    private static Shown Read(Rebb64Machine machine)
    {
        int screen = Bank + ((machine.PeekVic(0x18) >> 4) * 0x400);
        byte[] ram = machine.Ram.ToArray();

        byte Character(int column, int row) => ram[screen + (row * Columns) + column];

        string kind = Character(0, 0) switch
        {
            >= FirstFruit => nameof(ExtendScreen.Fruit),
            Space when Character(ExtendPicture.NameColumn, ExtendPicture.NameRow) >= FirstText => nameof(ExtendScreen.Text),
            Space => nameof(ExtendScreen.Cleared),
            _ => "level",
        };

        string Place(int place)
        {
            byte letter = Character(0, SidebarModel.FirstLetterRow + (place * 2));
            return letter is >= FirstLetter and < FirstFruit ? ((letter - FirstLetter) / 4).ToString(s_culture) : "-";
        }

        // A block's colour from its lower left: each turn of the screens (`$1853`) puts the corner's colour in the top two
        // cells of the first.
        string Block((int Column, int Row) block)
        {
            byte character = Character(block.Column * 2, block.Row * 2);
            int colour = machine.PeekColour((((block.Row * 2) + 1) * Columns) + (block.Column * 2)) & 0x0F;
            return $"{(character - FirstFruit) / 4:X2}/{colour:X}";
        }

        string roll = string.Concat(Enumerable.Range(0, SidebarModel.LetterCount).Select(Place));
        string fruit = string.Join(' ', Blocks().Select(Block));

        return new(kind, roll == "------" ? "none" : roll, fruit);
    }

    private static string PortKind(ExtendBonus.ExtendHold hold) => hold.Screen?.ToString() ?? "level";

    private static string PortRoll(ExtendBonus.ExtendHold hold)
        => hold.Roll is { } roll ? string.Concat(roll.Select(picture => picture.ToString(s_culture))) : "none";

    private static string PortFruit(ExtendBonus.ExtendHold hold)
    {
        string Block((int Column, int Row) block)
        {
            int index = (block.Row * ExtendPicture.FruitColumns) + block.Column;
            return $"{hold.Art[index]:X2}/{hold.ColourRam[index] & 0x0F:X}";
        }

        return string.Join(' ', Blocks().Select(Block));
    }

    // The blocks the box and its text leave whole.
    private static IEnumerable<(int Column, int Row)> Blocks()
        => from row in Enumerable.Range(0, ExtendPicture.FruitRows)
           from column in Enumerable.Range(0, ExtendPicture.FruitColumns)
           where OutsideBox(column * 2, row * 2)
           select (column, row);

    private static bool OutsideBox(int column, int row)
    {
        bool across = column + 1 < ExtendPicture.BoxLeft || column > ExtendPicture.BoxRight;
        bool down = row + 1 < ExtendPicture.BoxTop || row > ExtendPicture.BoxBottom;

        return across || down;
    }

    private sealed record Shown(string Kind, string Roll, string Fruit);
}
