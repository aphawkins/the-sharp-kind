// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Globalization;
using System.Numerics;
using BubbleBobbleSharp.Abstractions.Views;
using SharpKind;
using SharpKind.Graphics;

namespace BubbleBobbleSharp.Renditions.EightBit;

/// <summary>Draws the level.</summary>
internal sealed class PlayfieldView8Bit : IView<PlayfieldModel>
{
    private const int SourceCharacterWidth = 4;
    private const int CharacterHeight = 8;

    private const int TileSheetColumns = 10;

    private const int SourceBlockWidth = 8;
    private const int BlockHeight = 16;
    private const int SuperRoomCharacters = 0x20;
    private const int SuperRoomWalls = 5;

    private const string TileSheet = "LevelTiles";
    private const string EdgeSheet = "TileEdges";
    private const string SuperRoomSheet = "SuperRoomChars";
    private const string ItemSheet = "ItemChars";

    private readonly IGraphics _graphics;
    private readonly BbViewLayout _layout;
    private readonly LevelSheets _tiles;
    private readonly LevelSheets _edges;
    private readonly LevelSheets _superRoom;
    private readonly MulticolourSheet _blocks;
    private readonly FastColor _block;

    internal PlayfieldView8Bit(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        _graphics = surface.Graphics;
        _layout = surface.Layout;
        _tiles = new(surface, TileSheet);
        _edges = new(surface, EdgeSheet);
        _superRoom = new(surface, SuperRoomSheet);
        _blocks = new(surface, ItemSheet, ItemSheet + ".SuperRoom", transparent: true);

        // A multicolour block is all 11: the colour RAM's low three bits.
        _block = surface.Palette[(PlayfieldModel.WipedColourRam & 0x07).ToString(CultureInfo.InvariantCulture)];
    }

    public void Draw(PlayfieldModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        // Only the level coming in under the scroll's split starts below the top.
        bool incoming = model.FirstRow > 0;

        if (model.SuperRoom)
        {
            DrawSuperRoom(model, incoming);
            return;
        }

        string tiles = _tiles.Paint(model.Colours, incoming);
        string edges = _edges.Paint(model.Colours, incoming);

        for (int row = model.FirstRow; row < model.EndRow; row++)
        {
            int sourceRow = model.SourceRow(row);

            for (int column = 0; column < PlayfieldModel.Columns; column++)
            {
                byte character = model.Character(column, sourceRow);

                if (character == PlayfieldModel.Space)
                {
                    continue;
                }

                if (character == PlayfieldModel.Block)
                {
                    _graphics.DrawRectangleFilled(_layout.Playfield.Cell(column, row), _layout.CellWidth, _layout.CellHeight, _block);
                    continue;
                }

                string wipedTiles = model.IsWiped(column, sourceRow) ? _tiles.PaintWiped(model.Colours) : tiles;

                DrawCharacter(model, character, wipedTiles, edges, _layout.Playfield.Cell(column, row));
            }
        }
    }

    // The room's own characters, and its two blocks of item art. Its edges are blank (`$3665`), and `$1A` and `$1B` are the
    // scroll's.
    private void DrawSuperRoom(PlayfieldModel model, bool incoming)
    {
        string sheet = _superRoom.Paint(model.Colours, incoming, PlayfieldModel.SuperRoomColourRam);
        _blocks.Paint(model.Colours, PlayfieldModel.SuperRoomBlockColourRam);

        for (int row = model.FirstRow; row < model.EndRow; row++)
        {
            int sourceRow = model.SourceRow(row);

            for (int column = 0; column < PlayfieldModel.Columns; column++)
            {
                byte character = model.Character(column, sourceRow);
                Vector2 position = _layout.Playfield.Cell(column, row);

                if (character == PlayfieldModel.SuperRoomBlock)
                {
                    _graphics.DrawImagePart(
                        _blocks.Name,
                        position,
                        new(SourceBlockWidth * 2, BlockHeight),
                        new(PlayfieldModel.SuperRoomBlockArt * SourceBlockWidth, 0),
                        new(SourceBlockWidth, BlockHeight));
                    continue;
                }

                int index = character - PlayfieldModel.FirstSuperRoomCharacter;

                if (index is < 0 or >= SuperRoomCharacters)
                {
                    index = character - PlayfieldModel.FirstSuperRoomWall;

                    if (index is < 0 or >= SuperRoomWalls)
                    {
                        continue;
                    }
                }

                _graphics.DrawImagePart(
                    sheet,
                    position,
                    new(SourceCharacterWidth * 2, CharacterHeight),
                    new(index * SourceCharacterWidth, 0),
                    new(SourceCharacterWidth, CharacterHeight));
            }
        }
    }

    private void DrawCharacter(PlayfieldModel model, byte character, string tiles, string edges, Vector2 position)
    {
        (string sheet, int index, int columns) = character == PlayfieldModel.LevelTile
            ? (tiles, model.Tile, TileSheetColumns)
            : (edges, character - PlayfieldModel.FirstTileEdge, PlayfieldModel.TileEdgeCount);

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
