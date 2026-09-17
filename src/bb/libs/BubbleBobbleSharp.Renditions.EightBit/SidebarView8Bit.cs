// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Numerics;
using BubbleBobbleSharp.Abstractions.Views;
using SharpKind.Graphics;

namespace BubbleBobbleSharp.Renditions.EightBit;

/// <summary>
/// Draws the decoration running down both edges of the level: draw_border's
/// two-by-two block of characters, repeated from the top of the screen to the
/// bottom, at the level's leftmost two columns and its rightmost two.
/// <para>
/// The characters are C64 multicolour, so a character is four pixels wide in
/// the sheet and eight on screen - a multicolour pixel is two pixels wide.
/// The sheets are copied out of rebb64 as they stand, which is why the
/// doubling happens here rather than having been baked in: what is committed
/// stays byte for byte what the reference holds.
/// </para>
/// <para>
/// The decoration is inside the level rather than beside it, so it is painted
/// out of the same two background registers the level's own tiles are. Both
/// sheets are repainted in the level's colours before anything is drawn from
/// them - see <see cref="MulticolourSheet"/>.
/// </para>
/// </summary>
internal sealed class SidebarView8Bit : IView<SidebarModel>
{
    // A multicolour character: four pixels in the sheet, eight rows tall.
    private const int SourceCharacterWidth = 4;
    private const int CharacterHeight = 8;

    // Two characters across and two down, drawn at screen width, so sixteen by sixteen.
    private const int BlockColumns = 2;
    private const int BlockRows = 2;

    // The level tile sheet is a hundred characters in reading order, ten to the row - one per level,
    // which is what a level with no sidebar design repeats into all four of its characters.
    private const int TileSheetColumns = 10;

    private const string SidebarSheet = "Sidebars";
    private const string TileSheet = "LevelTiles";

    private readonly IGraphics _graphics;
    private readonly BbViewLayout _layout;
    private readonly MulticolourSheet _sidebars;
    private readonly MulticolourSheet _tiles;

    internal SidebarView8Bit(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        _graphics = surface.Graphics;
        _layout = surface.Layout;
        _sidebars = new(surface, SidebarSheet);
        _tiles = new(surface, TileSheet);
    }

    // $E10C. draw_border walks twelve two-row blocks down both edges and then writes the block's top
    // half once more on the last row, which is how twenty-five rows are covered by a block two rows tall.
    public void Draw(SidebarModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        _sidebars.Paint(model.Colours);
        _tiles.Paint(model.Colours);

        for (int row = 0; row < _layout.PlayfieldRows; row += BlockRows)
        {
            // The last row of the screen has no room for the block's lower half.
            int rows = Math.Min(BlockRows, _layout.PlayfieldRows - row);

            // The level's leftmost two columns and its rightmost two. init_level_renderer forces
            // the bitmap solid there - it ors the top two bits into the first byte of every row -
            // so the decoration always has wall beneath it rather than sitting over open space.
            DrawBlock(model, 0, row, rows);
            DrawBlock(model, _layout.PlayfieldColumns - BlockColumns, row, rows);
        }
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

    // A level with a design of its own takes one of that design's four characters, copied by the
    // routine at $E051; a level without repeats its header tile, which $E060 copies into all four
    // of the slots instead.
    private void DrawCharacter(SidebarModel model, int character, Vector2 position)
    {
        (string sheet, int index) = model.HasDesign
            ? (_sidebars.Name, (model.Design * SidebarModel.CharactersPerDesign) + character)
            : (_tiles.Name, model.HeaderTile);

        int columns = model.HasDesign ? SidebarModel.CharactersPerDesign : TileSheetColumns;
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
