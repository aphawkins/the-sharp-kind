// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Numerics;
using BubbleBobbleSharp.Abstractions.Views;
using SharpKind;
using SharpKind.Graphics;

namespace BubbleBobbleSharp.Renditions.EightBit;

/// <summary>Draws the decoration down both edges of the level.</summary>
internal sealed class SidebarView8Bit : IView<SidebarModel>
{
    private const int SourceCharacterWidth = 4;
    private const int CharacterHeight = 8;

    private const int BlockColumns = 2;
    private const int BlockRows = 2;

    private const int TileSheetColumns = 10;

    private const string NumberFont = "LevelNumber";
    private const int NumberRows = 6;
    private const string BlackColour = "0";
    private const string WhiteColour = "1";

    private const string SidebarSheet = "Sidebars";
    private const string TileSheet = "LevelTiles";

    private readonly IGraphics _graphics;
    private readonly BbViewLayout _layout;
    private readonly MulticolourSheet _sidebars;
    private readonly MulticolourSheet _tiles;
    private readonly FastColor _black;
    private readonly FastColor _white;

    internal SidebarView8Bit(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        _graphics = surface.Graphics;
        _layout = surface.Layout;
        _sidebars = new(surface, SidebarSheet);
        _tiles = new(surface, TileSheet);
        _black = surface.Palette[BlackColour];
        _white = surface.Palette[WhiteColour];
    }

    public void Draw(SidebarModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        _sidebars.Paint(model.Colours);
        _tiles.Paint(model.Colours);

        for (int row = 0; row < _layout.PlayfieldRows; row += BlockRows)
        {
            int rows = Math.Min(BlockRows, _layout.PlayfieldRows - row);

            DrawBlock(model, 0, row, rows);
            DrawBlock(model, _layout.PlayfieldColumns - BlockColumns, row, rows);
        }

        DrawNumber(model);
    }

    // $38E9: the level number's two characters ($1A, $1B) replace the top-left corner of the level.
    private void DrawNumber(SidebarModel model)
    {
        if (model.NumberText.Length == 0)
        {
            return;
        }

        // The font's background is transparent, so its rows are blacked out first: the five of the glyph and the blank
        // one under it ($386E-$3874 clear six). The rest of the cell keeps the wall: the reference fills those two rows
        // from the level's own data ($387A-$388F).
        _graphics.DrawRectangleFilled(
            _layout.Playfield.Cell(0, 0),
            BlockColumns * _layout.CellWidth,
            NumberRows * _layout.CellHeight / CharacterHeight,
            _black);
        _graphics.DrawTextLeft(_layout.Playfield.Cell(0, 0), model.NumberText, NumberFont, _white);
    }

    private void DrawBlock(SidebarModel model, int column, int row, int rows)
    {
        for (int blockRow = 0; blockRow < rows; blockRow++)
        {
            for (int blockColumn = 0; blockColumn < BlockColumns; blockColumn++)
            {
                DrawCharacter(
                    model,
                    (blockRow * BlockColumns) + blockColumn,
                    _layout.Playfield.Cell(column + blockColumn, row + blockRow));
            }
        }
    }

    private void DrawCharacter(SidebarModel model, int character, Vector2 position)
    {
        (string sheet, int index) = model.HasDesign
            ? (_sidebars.Name, (model.Design * SidebarModel.CharactersPerDesign) + character)
            : (_tiles.Name, model.HeaderTile);

        int columns = model.HasDesign ? SidebarModel.CharactersPerDesign : TileSheetColumns;
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
