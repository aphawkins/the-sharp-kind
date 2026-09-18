// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;

namespace BubbleBobbleSharpLib.Levels;

// The level half of setup_level_screen: the bit grid the renderer walks, and the characters it
// writes out of it. The model it produces is what crosses to a view.
internal static class Playfield
{
    // The characters a tile's edges and shadow are drawn with, named for where the renderer puts
    // them. Which one it picks depends on what the cell holds already, so the level's tiles join up
    // rather than each being outlined on its own.
    private const byte ShadowUnder = 0x0A;          // $E0EB, below a tile, over nothing
    private const byte ShadowUnderRight = 0x0B;     // $E0F5, below and to the right of a tile
    private const byte EdgeRight = 0x0C;            // $E0DD, right of a tile, over nothing
    private const byte ShadowUnderTile = 0x0D;      // $E0EF, below a tile, over another tile's shadow
    private const byte EdgeRightTile = 0x0E;        // $E0D7, right of a tile, beside another tile
    private const byte EdgeRightShadow = 0x0F;      // $E0DA, right of a tile, over a shadow

    // $E000. What the renderer leaves in screen memory, from the level and nothing else.
    internal static PlayfieldModel Build(Level level)
    {
        ArgumentNullException.ThrowIfNull(level);

        // $E014. The reference indexes its tables by a level counted from zero, and the tile sheet
        // holds one character per level. $E0CE splits the colour byte into the two nibbles the
        // background registers take; the byte itself crosses whole, since which nibble means what
        // is the rendition's business rather than the level's.
        return new(level.Number - 1, level.Colours, Characters(SolidMap.Build(level)));
    }

    // $E0B1. Every set bit puts the level's own tile on the screen, and three characters with it:
    // one to its right and two on the row below.
    private static byte[] Characters(SolidMap solid)
    {
        // A column and a row of slack. The renderer writes to the column after the one it is on and
        // the row below it, so a tile in the last column reaches screen column 32 and one in the last
        // row reaches the row past the screen. Neither is ever seen: draw_border clears column 32
        // straight afterwards, and the row is off the bottom.
        const int stride = SolidMap.Columns + 1;
        byte[] screen = new byte[stride * (SolidMap.Rows + 1)];

        // $E393. clear_color_ram, which fills screen memory with spaces despite the name.
        Array.Fill(screen, PlayfieldModel.Space);

        for (int row = 0; row < SolidMap.Rows; row++)
        {
            for (int column = 0; column < SolidMap.Columns; column++)
            {
                if (solid[row, column])
                {
                    Tile(screen, (row * stride) + column, stride);
                }
            }
        }

        // $E16F. The top row alone has its plain right-hand edges turned into the edge that stands
        // beside another tile. Nothing is above that row to cast the shadow the other one leaves
        // room for.
        for (int column = 0; column < SolidMap.Columns; column++)
        {
            screen[column] = screen[column] == EdgeRight ? EdgeRightTile : screen[column];
        }

        return Crop(screen, stride);
    }

    // $E0BD. One tile, and the three characters around it. Which of those three the edge and the
    // shadow are depends on what the cell holds already, so a row of tiles is edged once at its end
    // rather than at every tile in it.
    private static void Tile(byte[] screen, int cell, int stride)
    {
        screen[cell] = PlayfieldModel.LevelTile;

        byte right = screen[cell + 1];

        screen[cell + 1] = right == PlayfieldModel.Space
            ? EdgeRight
            : right is ShadowUnder or ShadowUnderTile ? EdgeRightShadow : EdgeRightTile;

        int below = cell + stride;

        screen[below] = screen[below] == PlayfieldModel.Space ? ShadowUnder : ShadowUnderTile;
        screen[below + 1] = ShadowUnderRight;
    }

    // The slack column and row go no further: what the view is handed is the screen as it is seen.
    private static byte[] Crop(byte[] screen, int stride)
    {
        byte[] cropped = new byte[SolidMap.Rows * SolidMap.Columns];

        for (int row = 0; row < SolidMap.Rows; row++)
        {
            Array.Copy(screen, row * stride, cropped, row * SolidMap.Columns, SolidMap.Columns);
        }

        return cropped;
    }
}
