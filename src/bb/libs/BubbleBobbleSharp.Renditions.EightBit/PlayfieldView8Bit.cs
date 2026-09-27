// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Numerics;
using BubbleBobbleSharp.Abstractions.Views;
using SharpKind.Graphics;

namespace BubbleBobbleSharp.Renditions.EightBit;

/// <summary>Draws the level.</summary>
internal sealed class PlayfieldView8Bit : IView<PlayfieldModel>
{
    private const int SourceCharacterWidth = 4;
    private const int CharacterHeight = 8;

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

        _graphics.DrawImagePart(
            sheet,
            position,
            new(SourceCharacterWidth * 2, CharacterHeight),
            new(sheetColumn * SourceCharacterWidth, sheetRow * CharacterHeight),
            new(SourceCharacterWidth, CharacterHeight));
    }
}
