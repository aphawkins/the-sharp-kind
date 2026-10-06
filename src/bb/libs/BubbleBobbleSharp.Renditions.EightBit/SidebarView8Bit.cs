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

    // $ACFD: the ROUND and READY!! lines, from column 12 of rows 10 and 12, in colour RAM $09: white where the font is.
    private const string ReadyFont = "Hud";
    private const int ReadyColumn = 12;
    private const int RoundRow = 10;
    private const int ReadyRow = 12;

    private const string SidebarSheet = "Sidebars";
    private const string TileSheet = "LevelTiles";

    // extend-chars.tga: E, X, T, N and D, one 8 by 16 block each, clear where the wall shows through.
    private const string LetterSheet = "ExtendChars";
    private const int LetterWidth = 8;
    private const int LetterHeight = 16;

    private readonly IGraphics _graphics;
    private readonly BbViewLayout _layout;
    private readonly LevelSheets _sidebars;
    private readonly LevelSheets _tiles;
    private readonly MulticolourSheet _letters;
    private readonly FastColor _black;
    private readonly FastColor _white;

    internal SidebarView8Bit(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        _graphics = surface.Graphics;
        _layout = surface.Layout;
        _sidebars = new(surface, SidebarSheet);
        _tiles = new(surface, TileSheet);
        _letters = new(surface, LetterSheet);
        _black = surface.Palette[BlackColour];
        _white = surface.Palette[WhiteColour];
    }

    public void Draw(SidebarModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        // Only the level coming in under the scroll's split starts below the top.
        bool incoming = model.FirstRow > 0;
        string sheet = model.HasDesign ? _sidebars.Paint(model.Colours, incoming) : _tiles.Paint(model.Colours, incoming);

        // A block's rows follow the decoration's own rows, wherever on the screen it has been moved to.
        for (int row = model.FirstRow; row < model.EndRow; row++)
        {
            int blockRow = (((row - model.Shift) % BlockRows) + BlockRows) % BlockRows;

            DrawRow(model, sheet, 0, row, blockRow);
            DrawRow(model, sheet, _layout.PlayfieldColumns - BlockColumns, row, blockRow);
        }

        if (model.Shift >= model.FirstRow && model.Shift < model.EndRow)
        {
            DrawNumber(model);
        }

        DrawLetters(model, sheet, 0, model.PlayerOneLetters);
        DrawLetters(model, sheet, _layout.PlayfieldColumns - BlockColumns, model.PlayerTwoLetters);

        DrawReady(model);
    }

    // $E6CD: each letter a player holds replaces a block of the wall, from row 10 down, two rows a letter. $E658 made
    // the block from the wall's four characters ($3940 copies them to $11-$14) taken a column at a time, so under a
    // letter the wall's top-right and bottom-left characters change places.
    private void DrawLetters(SidebarModel model, string sheet, int column, int letters)
    {
        if (letters == 0)
        {
            return;
        }

        _letters.Paint(model.Colours);

        for (int letter = 0; letter < SidebarModel.LetterCount; letter++)
        {
            if ((letters & (1 << letter)) == 0)
            {
                continue;
            }

            int row = SidebarModel.FirstLetterRow + (letter * BlockRows);

            for (int character = 0; character < BlockColumns * BlockRows; character++)
            {
                DrawCharacter(
                    model,
                    sheet,
                    character,
                    _layout.Playfield.Cell(column + (character / BlockRows), row + (character % BlockRows)));
            }

            _graphics.DrawImagePart(
                _letters.Name,
                _layout.Playfield.Cell(column, row),
                new(LetterWidth * 2, LetterHeight),
                new(SidebarModel.LetterPicture(letter) * LetterWidth, 0),
                new(LetterWidth, LetterHeight));
        }
    }

    // $3C01: the text is ORed into the characters under it, so the level shows through wherever the font is clear.
    private void DrawReady(SidebarModel model)
    {
        if (model.Round <= 0)
        {
            return;
        }

        _graphics.DrawTextLeft(_layout.Playfield.Cell(ReadyColumn, RoundRow), model.RoundText, ReadyFont, _white);
        _graphics.DrawTextLeft(_layout.Playfield.Cell(ReadyColumn, ReadyRow), model.ReadyText, ReadyFont, _white);
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
            _layout.Playfield.Cell(0, model.Shift),
            BlockColumns * _layout.CellWidth,
            NumberRows * _layout.CellHeight / CharacterHeight,
            _black);
        _graphics.DrawTextLeft(_layout.Playfield.Cell(0, model.Shift), model.NumberText, NumberFont, _white);
    }

    private void DrawRow(SidebarModel model, string sheet, int column, int row, int blockRow)
    {
        for (int blockColumn = 0; blockColumn < BlockColumns; blockColumn++)
        {
            DrawCharacter(
                model,
                sheet,
                (blockRow * BlockColumns) + blockColumn,
                _layout.Playfield.Cell(column + blockColumn, row));
        }
    }

    private void DrawCharacter(SidebarModel model, string sheet, int character, Vector2 position)
    {
        int index = model.HasDesign
            ? (model.Design * SidebarModel.CharactersPerDesign) + character
            : model.HeaderTile;

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
