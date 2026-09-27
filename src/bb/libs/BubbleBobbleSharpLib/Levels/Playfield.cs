// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;

namespace BubbleBobbleSharpLib.Levels;

internal static class Playfield
{
    private const byte ShadowUnder = 0x0A;
    private const byte ShadowUnderRight = 0x0B;
    private const byte EdgeRight = 0x0C;
    private const byte ShadowUnderTile = 0x0D;
    private const byte EdgeRightTile = 0x0E;
    private const byte EdgeRightShadow = 0x0F;

    internal static PlayfieldModel Build(Level level)
    {
        ArgumentNullException.ThrowIfNull(level);

        return new(level.Number - 1, level.Colours, Characters(SolidMap.Build(level, [])));
    }

    private static byte[] Characters(SolidMap solid)
    {
        const int stride = SolidMap.Columns + 1;
        byte[] screen = new byte[stride * (SolidMap.Rows + 1)];

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

        for (int column = 0; column < SolidMap.Columns; column++)
        {
            screen[column] = screen[column] == EdgeRight ? EdgeRightTile : screen[column];
        }

        return Crop(screen, stride);
    }

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
