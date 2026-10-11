// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Globalization;
using System.Numerics;
using BubbleBobbleSharp.Abstractions.Views;
using SharpKind;
using SharpKind.Graphics;

namespace BubbleBobbleSharp.Renditions.EightBit;

/// <summary>Draws a bonus room: the items on the level's platforms, the clock and the counts, and the results' window.</summary>
internal sealed class RoomView8Bit : IView<RoomModel>
{
    private const int SourceBlockWidth = 8;
    private const int BlockHeight = 16;

    private const int SourceCharacterWidth = 4;
    private const int CharacterHeight = 8;
    private const int TileSheetColumns = 10;

    private const string ItemSheet = "ItemChars";
    private const string TileSheet = "LevelTiles";
    private const string TextFont = "Small";

    private const int Black = 0;

    private readonly IViewSurface _surface;
    private readonly BbViewLayout _layout;
    private readonly IGraphics _graphics;
    private readonly MulticolourSheet _items;
    private readonly MulticolourSheet _tiles;

    internal RoomView8Bit(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        _surface = surface;
        _layout = surface.Layout;
        _graphics = surface.Graphics;
        _items = new(surface, ItemSheet, ItemSheet + ".Room", transparent: true);
        _tiles = new(surface, TileSheet, TileSheet + ".Room");
    }

    public void Draw(RoomModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (model.Cells.Count > 0)
        {
            _items.Paint(model.Colours, model.ColourRam);

            foreach ((int column, int row) in model.Cells)
            {
                _graphics.DrawImagePart(
                    _items.Name,
                    _layout.Playfield.Cell(column, row),
                    new(SourceBlockWidth * 2, BlockHeight),
                    new(model.Art * SourceBlockWidth, 0),
                    new(SourceBlockWidth, BlockHeight));
            }
        }

        if (model.Box)
        {
            DrawBox(model);
        }

        foreach (RoomText line in model.Lines)
        {
            _graphics.DrawTextLeft(
                _layout.Screen.Cell(line.Column, line.Row),
                line.Text,
                TextFont,
                Colour(line.Colour));
        }
    }

    // `$312E`: the window is the level's tile and, inside it, a space.
    private void DrawBox(RoomModel model)
    {
        _tiles.Paint(model.Colours);

        int tileColumn = model.Tile % TileSheetColumns;
        int tileRow = model.Tile / TileSheetColumns;

        for (int row = 0; row < RoomModel.BoxHeight; row++)
        {
            for (int column = 0; column < RoomModel.BoxWidth; column++)
            {
                bool inside = row > 0 && row < RoomModel.BoxHeight - 1 && column > 0 && column < RoomModel.BoxWidth - 1;
                Vector2 position = _layout.Screen.Cell(RoomModel.BoxColumn + column, RoomModel.BoxRow + row);

                if (inside)
                {
                    _graphics.DrawRectangleFilled(position, _layout.CellWidth, _layout.CellHeight, Colour(Black));
                    continue;
                }

                _graphics.DrawImagePart(
                    _tiles.Name,
                    position,
                    new(SourceCharacterWidth * 2, CharacterHeight),
                    new(tileColumn * SourceCharacterWidth, tileRow * CharacterHeight),
                    new(SourceCharacterWidth, CharacterHeight));
            }
        }
    }

    private FastColor Colour(int index) => _surface.Palette[index.ToString(CultureInfo.InvariantCulture)];
}
