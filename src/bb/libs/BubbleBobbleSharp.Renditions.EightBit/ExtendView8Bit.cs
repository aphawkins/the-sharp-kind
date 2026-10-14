// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Globalization;
using System.Numerics;
using BubbleBobbleSharp.Abstractions.Views;
using SharpKind;
using SharpKind.Graphics;

namespace BubbleBobbleSharp.Renditions.EightBit;

/// <summary>Draws the EXTEND bonus's screens over everything: black, the screen of fruit, or the player's text.</summary>
internal sealed class ExtendView8Bit : IView<ExtendPicture>
{
    private const int SourceBlockWidth = 8;
    private const int BlockHeight = 16;

    private const int SourceCharacterWidth = 4;
    private const int CharacterHeight = 8;
    private const int TileSheetColumns = 10;

    private const string ItemSheet = "ItemChars";
    private const string TileSheet = "LevelTiles";
    private const string LetterSheet = "ExtendChars";
    private const string TextFont = "Small";

    // The banner's letters are in the colour RAM `$A8E4` + `$14` gives them.
    private const int BannerColourRam = 0x0D;

    // A colour RAM byte's low three bits are all a multicolour cell takes from it: a sheet for each.
    private const int ColourRamEntries = 8;

    private const int Black = 0;

    private readonly IViewSurface _surface;
    private readonly BbViewLayout _layout;
    private readonly IGraphics _graphics;
    private readonly MulticolourSheet[] _items = new MulticolourSheet[ColourRamEntries];
    private readonly MulticolourSheet _tiles;
    private readonly MulticolourSheet _letters;

    internal ExtendView8Bit(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        _surface = surface;
        _layout = surface.Layout;
        _graphics = surface.Graphics;
        _tiles = new(surface, TileSheet, TileSheet + ".Extend");
        _letters = new(surface, LetterSheet, LetterSheet + ".Extend", transparent: true);

        for (int entry = 0; entry < ColourRamEntries; entry++)
        {
            string name = ItemSheet + ".Extend" + entry.ToString(CultureInfo.InvariantCulture);
            _items[entry] = new(surface, ItemSheet, name, transparent: true);
        }
    }

    public void Draw(ExtendPicture model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (model.Screen == ExtendScreen.Cleared)
        {
            _graphics.DrawRectangleFilled(
                _layout.Screen.Cell(0, 0),
                _layout.ScreenWidth,
                _layout.ScreenRows * _layout.CellHeight,
                Colour(Black));
            return;
        }

        _graphics.DrawRectangleFilled(
            _layout.Playfield.Cell(0, 0),
            _layout.PlayfieldColumns * _layout.CellWidth,
            _layout.ScreenRows * _layout.CellHeight,
            Colour(Black));

        if (model.Screen == ExtendScreen.Fruit)
        {
            DrawFruit(model);
            DrawBox(model);
            DrawBanner(model);
        }

        DrawText(model.NameText, ExtendPicture.NameColumn, ExtendPicture.NameRow);
        DrawText(ExtendPicture.GetsText, ExtendPicture.GetsColumn, ExtendPicture.GetsRow);
    }

    // `$3389`: a block of item art every two columns and rows, in its food's colour RAM; the first's top half in the corner's.
    private void DrawFruit(ExtendPicture model)
    {
        for (int row = 0; row < ExtendPicture.FruitRows; row++)
        {
            for (int column = 0; column < ExtendPicture.FruitColumns; column++)
            {
                MulticolourSheet sheet = _items[model.ColourRam(column, row) & (ColourRamEntries - 1)];
                sheet.Paint(model.Colours, model.ColourRam(column, row));

                _graphics.DrawImagePart(
                    sheet.Name,
                    _layout.Playfield.Cell(column * 2, row * 2),
                    new(SourceBlockWidth * 2, BlockHeight),
                    new(model.Art(column, row) * SourceBlockWidth, 0),
                    new(SourceBlockWidth, BlockHeight));
            }
        }

        MulticolourSheet corner = _items[ExtendPicture.CornerColourRam & (ColourRamEntries - 1)];
        corner.Paint(model.Colours, ExtendPicture.CornerColourRam);

        _graphics.DrawImagePart(
            corner.Name,
            _layout.Playfield.Cell(0, 0),
            new(SourceBlockWidth * 2, CharacterHeight),
            new(model.Art(0, 0) * SourceBlockWidth, 0),
            new(SourceBlockWidth, CharacterHeight));
    }

    // `$312E`: the box is the level's tile and, inside it, a space.
    private void DrawBox(ExtendPicture model)
    {
        _tiles.Paint(model.Colours);

        int tileColumn = model.Tile % TileSheetColumns;
        int tileRow = model.Tile / TileSheetColumns;

        for (int row = ExtendPicture.BoxTop; row <= ExtendPicture.BoxBottom; row++)
        {
            for (int column = ExtendPicture.BoxLeft; column <= ExtendPicture.BoxRight; column++)
            {
                Vector2 position = _layout.Playfield.Cell(column, row);

                if (ExtendPicture.IsInside(column, row))
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

    // `$33DA`: the sidebar's letters, a 2x2 block each.
    private void DrawBanner(ExtendPicture model)
    {
        _letters.Paint(model.Colours, BannerColourRam);

        for (int letter = 0; letter < ExtendPicture.Banner.Count; letter++)
        {
            _graphics.DrawImagePart(
                _letters.Name,
                _layout.Playfield.Cell(ExtendPicture.BannerColumn + (letter * 2), ExtendPicture.BannerRow),
                new(SourceBlockWidth * 2, BlockHeight),
                new(ExtendPicture.Banner[letter] * SourceBlockWidth, 0),
                new(SourceBlockWidth, BlockHeight));
        }
    }

    private void DrawText(string text, int column, int row)
        => _graphics.DrawTextLeft(_layout.Playfield.Cell(column, row), text, TextFont, Colour(ExtendPicture.TextColourRam));

    private FastColor Colour(int index) => _surface.Palette[index.ToString(CultureInfo.InvariantCulture)];
}
