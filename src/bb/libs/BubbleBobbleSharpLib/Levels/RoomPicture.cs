// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;

namespace BubbleBobbleSharpLib.Levels;

// What the room view draws, from the game.
internal static class RoomPicture
{
    // `$3234`: where a room shows its clock, at the top, and each player's count, along the bottom, and the colour RAM bytes
    // they are written with. Bit 3 makes them multicolour characters, whose own colour is the low three bits.
    private const int RoomClockColumn = 15;
    private const int RoomClockColour = 9;
    private const int RoomCountRow = 24;
    private const int RoomSecondColumn = 30;
    private const int RoomFirstColour = 13;
    private const int RoomSecondColour = 11;
    private const int CharacterColour = 0x07;

    // A bonus room over the level: the clock and the counts while it lasts, and the results after.
    internal static RoomModel Build(GameLoop loop, PlayfieldModel playfield)
    {
        ItemRoom room = loop.Room;

        if (loop.Ending.Blank)
        {
            return new([], 0, 0, playfield.Colours, playfield.Tile, loop.Ending.Lines, false);
        }

        if (!room.Active)
        {
            return new([], 0, 0, playfield.Colours, playfield.Tile, [], false);
        }

        List<RoomText> lines = [];

        if (room.Phase == ItemRoom.Stage.Playing)
        {
            lines.Add(new(RoomClockColumn, 0, RoomClockColour & CharacterColour, $"{loop.Timer.Seconds:D2}"));
            lines.Add(new(0, RoomCountRow, RoomFirstColour & CharacterColour, $"{room.Counts[0]:D2}"));
            lines.Add(new(RoomSecondColumn, RoomCountRow, RoomSecondColour & CharacterColour, $"{room.Counts[1]:D2}"));
        }

        lines.AddRange(room.Lines);

        return new(
            [.. room.Items.Where(item => item.X != 0 || item.Y != 0).Select(item => (item.Column, item.Row))],
            room.Art,
            room.Colour,
            playfield.Colours,
            playfield.Tile,
            lines,
            room.Phase == ItemRoom.Stage.Results);
    }
}
