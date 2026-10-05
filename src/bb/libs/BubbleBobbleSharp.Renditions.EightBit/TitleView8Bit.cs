// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Globalization;
using BubbleBobbleSharp.Abstractions.Views;
using SharpKind;

namespace BubbleBobbleSharp.Renditions.EightBit;

/// <summary>Draws a front-end screen: its stars, its rules and its text.</summary>
internal sealed class TitleView8Bit : IView<TitleModel>
{
    private const string TextFont = "Small";

    // The characters' multicolour pixels are two wide. $FE is one row of the background's second colour ($D022, blue),
    // $FF two rows of its third ($D023, cyan); $1E is two lines of blue, on rows 2 and 5.
    private const int StarColumn = 2;
    private const int StarRow = 3;
    private const int StarWidth = 2;
    private const int BlueColour = 6;
    private const int CyanColour = 3;

    private static readonly int[] s_ruleLines = [2, 5];

    private readonly IViewSurface _surface;
    private readonly FastColor _blue;
    private readonly FastColor _cyan;

    internal TitleView8Bit(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        _surface = surface;
        _blue = Colour(BlueColour);
        _cyan = Colour(CyanColour);
    }

    public void Draw(TitleModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        foreach (TitleStar star in model.Stars)
        {
            Pixels(star.Column, star.Row, StarColumn, StarRow, StarWidth, star.Big ? 2 : 1, star.Big ? _cyan : _blue);
        }

        foreach (TitleRule rule in model.Rules)
        {
            foreach (int line in s_ruleLines)
            {
                Pixels(rule.Column, rule.Row, 0, line, rule.Length * (int)_surface.Layout.CellWidth, 1, _blue);
            }
        }

        foreach (TitleText line in model.Lines)
        {
            _surface.Graphics.DrawTextLeft(
                _surface.Layout.Screen.Cell(line.Column, line.Row),
                line.Text,
                TextFont,
                Colour(line.Colour));
        }
    }

    private void Pixels(int column, int row, int x, int y, int width, int height, in FastColor colour)
        => _surface.Graphics.DrawRectangleFilled(
            _surface.Layout.Screen.Cell(column, row) + new System.Numerics.Vector2(x, y),
            width,
            height,
            colour);

    private FastColor Colour(int index) => _surface.Palette[index.ToString(CultureInfo.InvariantCulture)];
}
