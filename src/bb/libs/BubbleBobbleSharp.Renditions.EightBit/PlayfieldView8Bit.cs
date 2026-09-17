// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Numerics;
using BubbleBobbleSharp.Abstractions.Views;
using SharpKind.Graphics;

namespace BubbleBobbleSharp.Renditions.EightBit;

/// <summary>
/// Draws the level: the character the game left at each cell, out of the sheet
/// that character lives in.
/// <para>
/// Two sheets, because the C64 has two. The level's own tile is one of a
/// hundred in the tile sheet, one per level, and the six characters its edges
/// and shadow are drawn with come out of the charset, where the export cut
/// them into a sheet of their own. Both are multicolour, so both are four
/// pixels wide in the sheet and eight on screen.
/// </para>
/// <para>
/// The level's leftmost two columns and its rightmost two are drawn over by
/// the sidebar decoration, exactly as draw_border writes over them once the
/// level is on the screen. This view draws them anyway: doing the same work in
/// the same order is what keeps the two views from having to know about each
/// other.
/// </para>
/// <para>
/// Both sheets are repainted in the level's own colours before anything is
/// drawn from them - see <see cref="MulticolourSheet"/> - so what this draws
/// by name is the repainted copy rather than the sheet as it was exported.
/// </para>
/// </summary>
internal sealed class PlayfieldView8Bit : IView<PlayfieldModel>
{
    // A multicolour character: four pixels in the sheet, eight rows tall.
    private const int SourceCharacterWidth = 4;
    private const int CharacterHeight = 8;

    // The tile sheet is a hundred characters in reading order, ten to the row - one per level.
    private const int TileSheetColumns = 10;

    private const string TileSheet = "LevelTiles";
    private const string EdgeSheet = "TileEdges";

    private readonly IGraphics _graphics;
    private readonly BbViewLayout _layout;
    private readonly MulticolourSheet _tiles;
    private readonly MulticolourSheet _edges;

    internal PlayfieldView8Bit(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        _graphics = surface.Graphics;
        _layout = surface.Layout;
        _tiles = new(surface, TileSheet);
        _edges = new(surface, EdgeSheet);
    }

    public void Draw(PlayfieldModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        _tiles.Paint(model.Colours);
        _edges.Paint(model.Colours);

        for (int row = 0; row < PlayfieldModel.Rows; row++)
        {
            for (int column = 0; column < PlayfieldModel.Columns; column++)
            {
                byte character = model.Character(column, row);

                // An empty cell is the background showing through, and the background is drawn by
                // nobody: the C64 has it in a register rather than in the charset.
                if (character == PlayfieldModel.Space)
                {
                    continue;
                }

                DrawCharacter(model, character, _layout.Playfield.Cell(column, row));
            }
        }
    }

    private void DrawCharacter(PlayfieldModel model, byte character, Vector2 position)
    {
        (string sheet, int index, int columns) = character == PlayfieldModel.LevelTile
            ? (_tiles.Name, model.Tile, TileSheetColumns)
            : (_edges.Name, character - PlayfieldModel.FirstTileEdge, PlayfieldModel.TileEdgeCount);

        int sheetColumn = index % columns;
        int sheetRow = index / columns;

        // A multicolour pixel is two pixels wide, so the character is drawn at twice the width it
        // occupies in the sheet. The height is its own.
        _graphics.DrawImagePart(
            sheet,
            position,
            new(SourceCharacterWidth * 2, CharacterHeight),
            new(sheetColumn * SourceCharacterWidth, sheetRow * CharacterHeight),
            new(SourceCharacterWidth, CharacterHeight));
    }
}
