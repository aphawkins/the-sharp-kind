// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Globalization;
using System.Numerics;
using BubbleBobbleSharp.Abstractions.Views;
using SharpKind;
using SharpKind.Assets.Palettes;
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
    private const string HurrySheet = "HurryChars";

    // Entry 11 black, for the backing of the HURRY UP! text at row 11.
    private const int BackingColourRam = 0x08;

    private readonly IGraphics _graphics;
    private readonly BbViewLayout _layout;
    private readonly LevelSheets _tiles;
    private readonly LevelSheets _edges;
    private readonly LevelSheets _superRoom;
    private readonly MulticolourSheet _blocks;
    private readonly FastColor _block;
    private readonly IPaletteCollection _palette;
    private readonly MulticolourSheet _hurryTiles;
    private readonly MulticolourSheet _hurryEdges;
    private readonly MulticolourSheet _letters;
    private readonly MulticolourSheet _backing;

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
        _palette = surface.Palette;
        _block = Colour(PlayfieldModel.WipedColourRam);

        _hurryTiles = new(surface, TileSheet, TileSheet + "Hurry");
        _hurryEdges = new(surface, EdgeSheet, EdgeSheet + "Hurry");
        _letters = new(surface, HurrySheet, HurrySheet + ".Letters", transparent: true);
        _backing = new(surface, HurrySheet, HurrySheet + ".Backing", transparent: true);
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

        DrawHurry(model);
    }

    // `$3AB8`'s characters $37-$3F: the level's characters under the text, with the text's colour RAM, ORed with its letters; at
    // row 11 cleared to black around them first.
    private void DrawHurry(PlayfieldModel model)
    {
        if (model.Hurry is not HurryText hurry || hurry.Row < model.FirstRow || hurry.Row >= model.EndRow)
        {
            return;
        }

        _hurryTiles.Paint(model.Colours, hurry.ColourRam);
        _hurryEdges.Paint(model.Colours, hurry.ColourRam);
        _letters.Paint(model.Colours, hurry.ColourRam);
        _backing.Paint(model.Colours, BackingColourRam);

        int sourceRow = model.SourceRow(hurry.Row);

        for (int letter = 0; letter < HurryText.Length; letter++)
        {
            int column = HurryText.FirstColumn + letter;
            byte character = model.Character(column, sourceRow);
            Vector2 position = _layout.Playfield.Cell(column, hurry.Row);

            if (character == PlayfieldModel.Block)
            {
                _graphics.DrawRectangleFilled(position, _layout.CellWidth, _layout.CellHeight, Colour(hurry.ColourRam));
            }
            else if (character != PlayfieldModel.Space)
            {
                DrawCharacter(model, character, _hurryTiles.Name, _hurryEdges.Name, position);
            }

            if (hurry.Backed)
            {
                DrawLetter(_backing.Name, letter, CharacterHeight, position);
            }

            DrawLetter(_letters.Name, letter, 0, position);
        }
    }

    private void DrawLetter(string sheet, int letter, int sheetY, Vector2 position) => _graphics.DrawImagePart(
        sheet,
        position,
        new(SourceCharacterWidth * 2, CharacterHeight),
        new(letter * SourceCharacterWidth, sheetY),
        new(SourceCharacterWidth, CharacterHeight));

    private FastColor Colour(int colourRam) => _palette[(colourRam & 0x07).ToString(CultureInfo.InvariantCulture)];

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
